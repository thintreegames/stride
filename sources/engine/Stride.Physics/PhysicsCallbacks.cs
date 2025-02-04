using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;

namespace Stride.Physics
{
    public interface IContactEventHandler
    {
        void OnContactAdded<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold,
            in Vector3 contactOffset, in Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex) where TManifold : struct, IContactManifold<TManifold>;
        void OnContactRemoved<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold,
            in Vector3 contactOffset, in Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex) where TManifold : struct, IContactManifold<TManifold>;
        void OnAllContactRemoval(CollidableReference eventSource, CollidablePair pair);
    }

    public unsafe class ContactEvents<TEventHandler> : IDisposable where TEventHandler : IContactEventHandler
    {
        struct PreviousCollisionData
        {
            public CollidableReference Collidable;
            public bool Fresh;
            public int ContactCount;
            //FeatureIds are identifiers encoding what features on the involved shapes contributed to the contact. We store up to 4 feature ids, one for each potential contact.
            //A "feature" is things like a face, vertex, or edge. There is no single interpretation for what a feature is- the mapping is defined on a per collision pair level.
            //In this demo, we only care to check whether a given contact in the current frame maps onto a contact from a previous frame.
            //We can use this to only emit 'contact added' events when a new contact with an unrecognized id is reported.
            public int FeatureId0;
            public int FeatureId1;
            public int FeatureId2;
            public int FeatureId3;
        }

        Bodies bodies;
        public TEventHandler EventHandler;
        BufferPool pool;
        IThreadDispatcher threadDispatcher;


        QuickDictionary<CollidableReference, QuickList<PreviousCollisionData>, CollidableReferenceComparer> listeners;

        //Since the narrow phase works on multiple threads, we can't modify the collision data during execution.
        //The pending changes are stored in per-worker collections and flushed afterwards.
        struct PendingNewEntry
        {
            public int ListenerIndex;
            public PreviousCollisionData Collision;
        }

        QuickList<PendingNewEntry>[] pendingWorkerAdds;

        public ContactEvents(TEventHandler eventHandler, BufferPool pool, IThreadDispatcher threadDispatcher, int initialListenerCapacity = 32)
        {
            EventHandler = eventHandler;
            this.pool = pool;
            this.threadDispatcher = threadDispatcher;
            pendingWorkerAdds = new QuickList<PendingNewEntry>[threadDispatcher == null ? 1 : threadDispatcher.ThreadCount];
            listeners = new QuickDictionary<CollidableReference, QuickList<PreviousCollisionData>, CollidableReferenceComparer>(initialListenerCapacity, pool);
        }

        public void Initialize(BepuPhysics.Simulation simulation)
        {
            bodies = simulation.Bodies;
        }

        /// <summary>
        /// Begins listening for events related to the given collidable.
        /// </summary>
        /// <param name="collidable">Collidable to monitor for events.</param>
        public void RegisterListener(CollidableReference collidable)
        {
            listeners.Add(collidable, default, pool);
        }

        /// <summary>
        /// Stops listening for events related to the given collidable.
        /// </summary>
        /// <param name="collidable">Collidable to stop listening for.</param>
        public void UnregisterListener(CollidableReference collidable)
        {
            var exists = listeners.GetTableIndices(ref collidable, out var tableIndex, out var elementIndex);
            Debug.Assert(exists, "Should only try to unregister listeners that actually exist.");
            listeners.Values[elementIndex].Dispose(pool);
            listeners.FastRemove(tableIndex, elementIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool FeatureIdContained(int featureId, ulong previousFeatureIds)
        {
            var contained0 = (((int)previousFeatureIds ^ featureId) & 0xFFFF) == 0;
            var contained1 = (((int)(previousFeatureIds >> 16) ^ featureId) & 0xFFFF) == 0;
            var contained2 = (((int)(previousFeatureIds >> 32) ^ featureId) & 0xFFFF) == 0;
            var contained3 = (((int)(previousFeatureIds >> 48) ^ featureId) & 0xFFFF) == 0;
            return contained0 | contained1 | contained2 | contained3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdatePreviousCollision<TManifold>(ref PreviousCollisionData collision, ref TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
        {
            Debug.Assert(manifold.Count <= 4, "This demo was built on the assumption that nonconvex manifolds will have a maximum of four contacts, but that might have changed.");
            //If the above assert gets hit because of a change to nonconvex manifold capacities, the packed feature id representation this uses will need to be updated.
            //I very much doubt the nonconvex manifold will ever use more than 8 contacts, so addressing this wouldn't require much of a change.
            for (int j = 0; j < manifold.Count; ++j)
            {
                Unsafe.Add(ref collision.FeatureId0, j) = manifold.GetFeatureId(j);
            }
            collision.ContactCount = manifold.Count;
            collision.Fresh = true;
        }

        void HandleManifoldForCollidable<TManifold>(int workerIndex, CollidableReference source, CollidableReference other, CollidablePair pair, ref TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
        {
            //The "source" refers to the object that an event handler was (potentially) attached to, so we look for listeners registered for it.
            //(This function is called for both orders of the pair, so we'll catch listeners for either.)
            if (listeners.GetTableIndices(ref source, out var tableIndex, out var listenerIndex))
            {
                //This collidable is registered. Is the opposing collidable present?
                ref var previousCollisions = ref listeners.Values[listenerIndex];
                int previousCollisionIndex = -1;
                for (int i = 0; i < previousCollisions.Count; ++i)
                {
                    ref var collision = ref previousCollisions[i];
                    // Since the 'Packed' field contains both the handle type (dynamic, kinematic, or static) and the handle index packed into a single bitfield, an equal value guarantees we are dealing with the same collidable.
                    if (collision.Collidable.Packed == other.Packed)
                    {
                        previousCollisionIndex = i;
                        // This manifold is associated with an existing collision.
                        for (int contactIndex = 0; contactIndex < manifold.Count; ++contactIndex)
                        {
                            // We can check if each contact was already present in the previous frame by looking at contact feature ids. See the 'PreviousCollisionData' for a little more info on FeatureIds.
                            var featureId = manifold.GetFeatureId(contactIndex);
                            var featureIdIsOld = false;
                            for (int previousContactIndex = 0; previousContactIndex < collision.ContactCount; ++previousContactIndex)
                            {
                                if (featureId == Unsafe.Add(ref collision.FeatureId0, previousContactIndex))
                                {
                                    featureIdIsOld = true;
                                    break;
                                }
                            }

                            if (!featureIdIsOld)
                            {
                                manifold.GetContact(contactIndex, out var offset, out var normal, out var depth, out _);
                                EventHandler.OnContactAdded(source, pair, ref manifold, offset, normal, depth, featureId, contactIndex, workerIndex);
                            }
                            else
                            {
                                manifold.GetContact(contactIndex, out var offset, out var normal, out var depth, out var _);
                                if (depth < -1e-3f)
                                {
                                    EventHandler.OnContactRemoved(source, pair, ref manifold, offset, normal, depth, featureId, contactIndex, workerIndex);
                                }
                            }
                        }

                        UpdatePreviousCollision(ref collision, ref manifold);
                        break;
                    }
                }

                if (previousCollisionIndex < 0)
                {
                    //There was no collision previously.
                    ref var addsforWorker = ref pendingWorkerAdds[workerIndex];

                    //EnsureCapacity will create the list if it doesn't already exist.
                    addsforWorker.EnsureCapacity(Math.Max(addsforWorker.Count + 1, 64), threadDispatcher != null ? threadDispatcher.GetThreadMemoryPool(workerIndex) : pool);
                    ref var pendingAdd = ref addsforWorker.AllocateUnsafely();
                    pendingAdd.ListenerIndex = listenerIndex;
                    pendingAdd.Collision.Collidable = other;
                    UpdatePreviousCollision(ref pendingAdd.Collision, ref manifold);

                    //Dispatch events for all contacts in this new manifold.
                    for (int i = 0; i < manifold.Count; ++i)
                    {
                        manifold.GetContact(i, out var offset, out var normal, out var depth, out var featureId);
                        EventHandler.OnContactAdded(source, pair, ref manifold, offset, normal, depth, featureId, i, workerIndex);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HandleManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold) where TManifold : struct, IContactManifold<TManifold>
        {
            HandleManifoldForCollidable(workerIndex, pair.A, pair.B, pair, ref manifold);
        }

        public void Flush()
        {
            //For simplicity, this is completely sequential. Note that it's technically possible to extract more parallelism, but the complexity cost is high and you would need
            //very large numbers of events being processed to make it worth it.

            //Remove any stale collisions. Stale collisions are those which should have received a new manifold update but did not because the manifold is no longer active.
            for (int i = 0; i < listeners.Count; ++i)
            {
                var collidable = listeners.Keys[i];
                //Pairs involved with inactive bodies do not need to be checked for freshness. If we did, it would result in inactive manifolds being considered a removal, and 
                //more contact added events would fire when the bodies woke up.
                if (collidable.Mobility != CollidableMobility.Static && bodies.HandleToLocation[collidable.BodyHandle.Value].SetIndex > 0)
                    continue;
                ref var collisions = ref listeners.Values[i];
                //Note reverse order. We remove during iteration.
                for (int j = collisions.Count - 1; j >= 0; --j)
                {
                    ref var collision = ref collisions[j];
                    //Again, any pair involving inactive bodies does not need to be examined.
                    if (collision.Collidable.Mobility != CollidableMobility.Static && bodies.HandleToLocation[collision.Collidable.BodyHandle.Value].SetIndex > 0)
                        continue;

                    if (!collision.Fresh)
                    {
                        EventHandler.OnAllContactRemoval(collidable, new CollidablePair(collidable, collision.Collidable));

                        //This collision was not updated since the last flush despite being active. It should be removed.
                        collisions.FastRemoveAt(j);
                        if (collisions.Count == 0)
                        {
                            collisions.Dispose(pool);
                            collisions = default;
                        }
                    }
                    else
                    {
                        collision.Fresh = false;
                    }
                }
            }

            for (int i = 0; i < pendingWorkerAdds.Length; ++i)
            {
                ref var pendingAdds = ref pendingWorkerAdds[i];
                for (int j = 0; j < pendingAdds.Count; ++j)
                {
                    ref var add = ref pendingAdds[j];
                    ref var collisions = ref listeners.Values[add.ListenerIndex];
                    //Ensure capacity will initialize the slot if necessary.
                    collisions.EnsureCapacity(Math.Max(8, collisions.Count + 1), pool);
                    collisions.AllocateUnsafely() = pendingAdds[j].Collision;
                }
                if (pendingAdds.Span.Allocated)
                    pendingAdds.Dispose(threadDispatcher == null ? pool : threadDispatcher.GetThreadMemoryPool(i));
                //We rely on zeroing out the count for lazy initialization.
                pendingAdds = default;
            }
        }

        public void Dispose()
        {
            listeners.Dispose(pool);
            for (int i = 0; i < pendingWorkerAdds.Length; ++i)
            {
                Debug.Assert(!pendingWorkerAdds[i].Span.Allocated, "The pending worker adds should have been disposed by the previous flush.");
            }
        }
    }


    public struct SimplePoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        /// <summary>
        /// Gravity to apply to dynamic bodies in the simulation.
        /// </summary>
        public Vector3 Gravity;
        /// <summary>
        /// Fraction of dynamic body linear velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.
        /// </summary>
        public float LinearDamping;
        /// <summary>
        /// Fraction of dynamic body angular velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.
        /// </summary>
        public float AngularDamping;

        public CollidableProperty<SimplePhysicsBodySettings> PhysicsBodySettings;

        private Vector3 gravityDt;
        private float linearDampingDt;
        private float angularDampingDt;
        private BepuPhysics.Simulation simulation;

        private float deltaTime;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        /// <summary>
        /// Creates a new set of simple callbacks for the demos.
        /// </summary>
        /// <param name="gravity">Gravity to apply to dynamic bodies in the simulation.</param>
        /// <param name="linearDamping">Fraction of dynamic body linear velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.</param>
        /// <param name="angularDamping">Fraction of dynamic body angular velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.</param>
        public SimplePoseIntegratorCallbacks(Vector3 gravity, float linearDamping = .03f, float angularDamping = .03f)
            : this()
        {
            Gravity = gravity;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
        }

        public void Initialize(BepuPhysics.Simulation simulation)
        {
            this.simulation = simulation;
            PhysicsBodySettings.Initialize(simulation);
        }

        public void PrepareForIntegration(float dt)
        {
            deltaTime = dt;
            //No reason to recalculate gravity * dt for every body; just cache it ahead of time.
            gravityDt = Gravity * dt;
            //Since these callbacks don't use per-body damping values, we can precalculate everything.
            linearDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt);
            angularDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia,
            int workerIndex, ref BodyVelocity velocity)
        {
            //Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
            if (localInertia.InverseMass > 0)
            {
                var bodySettings = PhysicsBodySettings[simulation.Bodies.ActiveSet.IndexToHandle[bodyIndex]];

                if (bodySettings.CustomGravity)
                {
                    gravityDt = bodySettings.Gravity * deltaTime;
                }

                if (bodySettings.CustomDamping)
                {
                    linearDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - bodySettings.LinearDamping, 0, 1), deltaTime);
                    angularDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - bodySettings.AngularDamping, 0, 1), deltaTime);
                }

                velocity.Linear = (velocity.Linear + gravityDt) * linearDampingDt;
                velocity.Angular = velocity.Angular * angularDampingDt;
            }
        }

    }

    public struct SimpleNarrowPhaseCallbacks<TEventHandler> : INarrowPhaseCallbacks where TEventHandler : IContactEventHandler
    {
        public CharacterControllers Characters;
        public CollidableProperty<SimplePhysicsMaterial> PhysicsMaterials;
        public CollidableProperty<CollisionProperty> CollisionProps;
        public ContactEvents<TEventHandler> Events;


        public void Initialize(BepuPhysics.Simulation simulation)
        {
            Characters.Initialize(simulation);
            PhysicsMaterials.Initialize(simulation);
            CollisionProps.Initialize(simulation);
            Events.Initialize(simulation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
        {
            var collisionPropA = CollisionProps[a];
            var collisionPropB = CollisionProps[b];

            if (collisionPropA.CollisionEnabled == CollisionType.NoCollision || collisionPropB.CollisionEnabled == CollisionType.NoCollision)
            {
                return false;
            }

            return (collisionPropA.ObjectType == CollisionObjectType.WorldStatic && collisionPropB.WorldStatic != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.WorldStatic && collisionPropA.WorldStatic != CollisionResponseTypes.Ignore)

                || (collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && collisionPropB.WorldDynamic != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && collisionPropA.WorldDynamic != CollisionResponseTypes.Ignore)

                || (collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && collisionPropB.PhysicsBody != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && collisionPropA.PhysicsBody != CollisionResponseTypes.Ignore)

                || (collisionPropA.ObjectType == CollisionObjectType.Pawn && collisionPropB.Pawn != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.Pawn && collisionPropA.Pawn != CollisionResponseTypes.Ignore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            var collisionPropA = CollisionProps[pair.A];
            var collisionPropB = CollisionProps[pair.B];

            if (collisionPropA.CollisionEnabled == CollisionType.NoCollision || collisionPropB.CollisionEnabled == CollisionType.NoCollision)
            {
                return false;
            }

            return (collisionPropA.ObjectType == CollisionObjectType.WorldStatic && collisionPropB.WorldStatic != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.WorldStatic && collisionPropA.WorldStatic != CollisionResponseTypes.Ignore)

                || (collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && collisionPropB.WorldDynamic != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && collisionPropA.WorldDynamic != CollisionResponseTypes.Ignore)

                || (collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && collisionPropB.PhysicsBody != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && collisionPropA.PhysicsBody != CollisionResponseTypes.Ignore)

                || (collisionPropA.ObjectType == CollisionObjectType.Pawn && collisionPropB.Pawn != CollisionResponseTypes.Ignore)
                || (collisionPropB.ObjectType == CollisionObjectType.Pawn && collisionPropA.Pawn != CollisionResponseTypes.Ignore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold>
        {
            var physicsMaterialA = PhysicsMaterials[pair.A];
            var physicsMaterialB = PhysicsMaterials[pair.B];
            var collisionPropA = CollisionProps[pair.A];
            var collisionPropB = CollisionProps[pair.B];

            pairMaterial.FrictionCoefficient = physicsMaterialA.FrictionCoefficient * physicsMaterialB.FrictionCoefficient;
            pairMaterial.MaximumRecoveryVelocity = MathF.Max(physicsMaterialA.MaximumRecoveryVelocity, physicsMaterialB.MaximumRecoveryVelocity);
            pairMaterial.SpringSettings = pairMaterial.MaximumRecoveryVelocity == physicsMaterialA.MaximumRecoveryVelocity ? physicsMaterialA.SpringSettings : physicsMaterialB.SpringSettings;
            Characters.TryReportContacts(pair, ref manifold, workerIndex, ref pairMaterial);

            if (collisionPropA.GenerateOverlapEvents || collisionPropB.GenerateOverlapEvents)
            {
                if (collisionPropA.ObjectType == CollisionObjectType.WorldStatic && (collisionPropB.WorldStatic == CollisionResponseTypes.Overlap || collisionPropB.WorldStatic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldStatic && (collisionPropA.WorldStatic == CollisionResponseTypes.Overlap || collisionPropA.WorldStatic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropB.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropB.WorldDynamic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropA.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropA.WorldDynamic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.Pawn && (collisionPropB.Pawn == CollisionResponseTypes.Overlap || collisionPropB.Pawn == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.Pawn && (collisionPropA.Pawn == CollisionResponseTypes.Overlap || collisionPropA.Pawn == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropB.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropB.PhysicsBody == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropA.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropA.PhysicsBody == CollisionResponseTypes.Block))
                {
                    if (collisionPropA.GenerateOverlapEvents)
                    {
                        Events.HandleManifold(workerIndex, pair, ref manifold);
                    }

                    if (collisionPropB.GenerateOverlapEvents)
                    {
                        Events.HandleManifold(workerIndex, new CollidablePair(pair.B, pair.A), ref manifold);
                    }
                }


            }

            if (collisionPropA.CollisionEnabled == CollisionType.QueryOnly || collisionPropB.CollisionEnabled == CollisionType.QueryOnly)
            {
                return false;
            }

            return collisionPropA.ObjectType == CollisionObjectType.WorldStatic && collisionPropB.WorldStatic == CollisionResponseTypes.Block
                || collisionPropB.ObjectType == CollisionObjectType.WorldStatic && collisionPropA.WorldStatic == CollisionResponseTypes.Block

                || collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && collisionPropB.WorldDynamic == CollisionResponseTypes.Block
                || collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && collisionPropA.WorldDynamic == CollisionResponseTypes.Block

                || collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && collisionPropB.PhysicsBody == CollisionResponseTypes.Block
                || collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && collisionPropA.PhysicsBody == CollisionResponseTypes.Block

                || collisionPropA.ObjectType == CollisionObjectType.Pawn && collisionPropB.Pawn == CollisionResponseTypes.Block
                || collisionPropB.ObjectType == CollisionObjectType.Pawn && collisionPropA.Pawn == CollisionResponseTypes.Block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
            Characters.Dispose();
        }
    }
}
