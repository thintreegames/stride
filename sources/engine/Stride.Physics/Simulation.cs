// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics.Engine;
using Stride.Physics.Events;
using Stride.Rendering;

namespace Stride.Physics
{
    public class Simulation : IDisposable
    {
        public struct ContactEventHandler : IContactEventHandler
        {
            private Simulation simulation;

            private Dictionary<uint, List<int>> contactsColliding;

            public ContactEventHandler(Simulation simulation)
            {
                this.simulation = simulation;
                contactsColliding = new Dictionary<uint, List<int>>();
            }

            public void OnContactAdded<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold,
                in System.Numerics.Vector3 contactOffset, in System.Numerics.Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex) where TManifold : struct, IContactManifold<TManifold>
            {
                var collisionPropA = simulation.collisionProps[pair.A];
                var collisionPropB = simulation.collisionProps[pair.B];

                if (collisionPropA.ObjectType == CollisionObjectType.WorldStatic && (collisionPropB.WorldStatic == CollisionResponseTypes.Overlap || collisionPropB.WorldStatic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldStatic && (collisionPropA.WorldStatic == CollisionResponseTypes.Overlap || collisionPropA.WorldStatic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropB.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropB.WorldDynamic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropA.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropA.WorldDynamic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.Pawn && (collisionPropB.Pawn == CollisionResponseTypes.Overlap || collisionPropB.Pawn == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.Pawn && (collisionPropA.Pawn == CollisionResponseTypes.Overlap || collisionPropA.Pawn == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropB.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropB.PhysicsBody == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropA.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropA.PhysicsBody == CollisionResponseTypes.Block))
                {
                    var physicsTagsA = simulation.physicsTags[pair.A];
                    var physicsTagsB = simulation.physicsTags[pair.B];

                    if (!simulation.PhysicsProcessor.TryGetComponentById(physicsTagsA.ComponentId, out var componentA)) return;
                    if (!simulation.PhysicsProcessor.TryGetComponentById(physicsTagsB.ComponentId, out var componentB)) return;

                    if (!contactsColliding.TryGetValue(pair.A.Packed, out var contactList) || contactList.Count == 0)
                    {
                        simulation.physicsEventQueue.Post(new PhysicsEvent
                        {
                            ActingComponent = componentA,
                            PhysicsEventData = new OnColliderEnter
                            {
                                Component = componentB,
                            }
                        });

                        if (contactList == null)
                        {
                            contactsColliding.Add(pair.A.Packed, contactList = new List<int>());
                        }
                    }

                    if (contactList.Contains(contactIndex)) return;

                    contactList.Add(contactIndex);

                    simulation.physicsEventQueue.Post(new PhysicsEvent
                    {
                        ActingComponent = componentA,
                        PhysicsEventData = new ContactAddedEvent
                        {
                            Component = componentB,
                            ContactOffset = new Vector3(contactOffset.X, contactOffset.Y, contactOffset.Z),
                            ContactNormal = new Vector3(contactNormal.X, contactNormal.Y, contactNormal.Z),
                            Depth = depth
                        }
                    });
                }
            }

            public void OnContactRemoved<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold,
                in System.Numerics.Vector3 contactOffset, in System.Numerics.Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex) where TManifold : struct, IContactManifold<TManifold>
            {
                var collisionPropA = simulation.collisionProps[pair.A];
                var collisionPropB = simulation.collisionProps[pair.B];

                if (collisionPropA.ObjectType == CollisionObjectType.WorldStatic && (collisionPropB.WorldStatic == CollisionResponseTypes.Overlap || collisionPropB.WorldStatic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldStatic && (collisionPropA.WorldStatic == CollisionResponseTypes.Overlap || collisionPropA.WorldStatic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropB.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropB.WorldDynamic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropA.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropA.WorldDynamic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.Pawn && (collisionPropB.Pawn == CollisionResponseTypes.Overlap || collisionPropB.Pawn == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.Pawn && (collisionPropA.Pawn == CollisionResponseTypes.Overlap || collisionPropA.Pawn == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropB.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropB.PhysicsBody == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropA.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropA.PhysicsBody == CollisionResponseTypes.Block))
                {
                    var physicsTagsA = simulation.physicsTags[pair.A];
                    var physicsTagsB = simulation.physicsTags[pair.B];

                    if (!simulation.PhysicsProcessor.TryGetComponentById(physicsTagsA.ComponentId, out var componentA)) return;
                    if (!simulation.PhysicsProcessor.TryGetComponentById(physicsTagsB.ComponentId, out var componentB)) return;

                    if (contactsColliding.TryGetValue(pair.A.Packed, out var contactList) && contactList.Count != 0)
                    {
                        if (contactList.Remove(contactIndex))
                        {
                            simulation.physicsEventQueue.Post(new PhysicsEvent
                            {
                                ActingComponent = componentA,
                                PhysicsEventData = new ContactRemovedEvent
                                {
                                    Component = componentB,
                                    ContactOffset = new Vector3(contactOffset.X, contactOffset.Y, contactOffset.Z),
                                    ContactNormal = new Vector3(contactNormal.X, contactNormal.Y, contactNormal.Z),
                                    Depth = depth
                                }
                            });
                        }

                        if (contactList.Count == 0)
                        {
                            simulation.physicsEventQueue.Post(new PhysicsEvent
                            {
                                ActingComponent = componentA,
                                PhysicsEventData = new OnColliderExit
                                {
                                    Component = componentB,
                                }
                            });
                        }
                    }
                }
            }

            public void OnAllContactRemoval(CollidableReference eventSource, CollidablePair pair)
            {
                var collisionPropA = simulation.collisionProps[pair.A];
                var collisionPropB = simulation.collisionProps[pair.B];


                if (collisionPropA.ObjectType == CollisionObjectType.WorldStatic && (collisionPropB.WorldStatic == CollisionResponseTypes.Overlap || collisionPropB.WorldStatic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldStatic && (collisionPropA.WorldStatic == CollisionResponseTypes.Overlap || collisionPropA.WorldStatic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropB.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropB.WorldDynamic == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.WorldDynamic && (collisionPropA.WorldDynamic == CollisionResponseTypes.Overlap || collisionPropA.WorldDynamic == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.Pawn && (collisionPropB.Pawn == CollisionResponseTypes.Overlap || collisionPropB.Pawn == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.Pawn && (collisionPropA.Pawn == CollisionResponseTypes.Overlap || collisionPropA.Pawn == CollisionResponseTypes.Block)

                    || collisionPropA.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropB.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropB.PhysicsBody == CollisionResponseTypes.Block)
                    || collisionPropB.ObjectType == CollisionObjectType.PhysicsBody && (collisionPropA.PhysicsBody == CollisionResponseTypes.Overlap || collisionPropA.PhysicsBody == CollisionResponseTypes.Block))
                {
                    if (contactsColliding.TryGetValue(eventSource.Packed, out var contactList) && contactList.Count != 0)
                    {
                        contactList.Clear();

                        var physicsTagsA = simulation.physicsTags[pair.A];
                        var physicsTagsB = simulation.physicsTags[pair.B];

                        if (!simulation.PhysicsProcessor.TryGetComponentById(physicsTagsA.ComponentId, out var componentA)) return;
                        if (!simulation.PhysicsProcessor.TryGetComponentById(physicsTagsB.ComponentId, out var componentB)) return;

                        simulation.physicsEventQueue.Post(new PhysicsEvent
                        {
                            ActingComponent = componentA,
                            PhysicsEventData = new OnColliderExit
                            {
                                Component = componentB,
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Totally disable the simulation if set to true
        /// </summary>
        public static bool DisableSimulation = false;

        public PhysicsProcessor PhysicsProcessor { get; private set; }

        public BufferPool BufferPool { get; private set; }


        private BepuPhysics.Simulation simulation;
        private CharacterControllers characters;
        private ContactEvents<ContactEventHandler> contactEventHandler;

        private BufferBlock<PhysicsEvent> physicsEventQueue;

        internal CollidableProperty<SimplePhysicsMaterial> physicsMaterials;
        internal CollidableProperty<SimplePhysicsBodySettings> physicsBodySettings;
        internal CollidableProperty<SimplePhysicsTag> physicsTags;
        internal CollidableProperty<CollisionProperty> collisionProps;

        public Simulation(PhysicsProcessor physicsProcessor, BufferPool bufferPool, SimpleThreadDispatcher threadDispatcher)
        {
            PhysicsProcessor = physicsProcessor;
            BufferPool = bufferPool;

            contactEventHandler = new ContactEvents<ContactEventHandler>(new ContactEventHandler(this), BufferPool, threadDispatcher);
            physicsEventQueue = new BufferBlock<PhysicsEvent>();

            characters = new CharacterControllers(bufferPool);

            //The PositionFirstTimestepper is the simplest timestepping mode, but since it integrates velocity into position at the start of the frame, directly modified velocities outside of the timestep
            //will be integrated before collision detection or the solver has a chance to intervene. That's fine in this demo. Other built-in options include the PositionLastTimestepper and the SubsteppingTimestepper.
            //Note that the timestepper also has callbacks that you can use for executing logic between processing stages, like BeforeCollisionDetection.
            physicsMaterials = new CollidableProperty<SimplePhysicsMaterial>();
            physicsBodySettings = new CollidableProperty<SimplePhysicsBodySettings>();
            physicsTags = new CollidableProperty<SimplePhysicsTag>();
            collisionProps = new CollidableProperty<CollisionProperty>();

            var narrowPhaseCallbacks = new SimpleNarrowPhaseCallbacks<ContactEventHandler>()
            {
                Characters = characters,
                PhysicsMaterials = physicsMaterials,
                CollisionProps = collisionProps,
                Events = contactEventHandler
            };

            var poseCallbacks = new SimplePoseIntegratorCallbacks(new System.Numerics.Vector3(0, -9.81f, 0))
            {
                PhysicsBodySettings = physicsBodySettings
            };

            simulation = BepuPhysics.Simulation.Create(bufferPool,
                   narrowPhaseCallbacks,
                   poseCallbacks,
                   new PositionFirstTimestepper());

            physicsTags.Initialize(simulation);
        }

        public void Simulate(double dt, IThreadDispatcher threadDispatcher = null)
        {
            simulation.Timestep(1 / 60f, threadDispatcher);
            contactEventHandler.Flush();
        }

        public void SendEvents()
        {
            while(physicsEventQueue.TryReceive(out var physicsEvent))
            {
                if (physicsEvent.ActingComponent.physicsEventCallbacks.TryGetValue(physicsEvent.PhysicsEventData.GetType(), out var handlers))
                {
                    foreach (Delegate handler in handlers)
                        handler.DynamicInvoke(physicsEvent.ActingComponent, physicsEvent.PhysicsEventData);
                }
            }
        }

        internal ref CharacterController AllocateCharacter(BodyHandle bodyHandle)
        {
            return ref characters.AllocateCharacter(bodyHandle);
        }

        internal ref CharacterController GetCharacterByBodyHandle(BodyHandle bodyHandle)
        {
            return ref characters.GetCharacterByBodyHandle(bodyHandle);
        }

        internal void RegisterOverlapListener(StaticHandle staticHandle)
        {
            contactEventHandler.RegisterListener(new CollidableReference(staticHandle));
        }

        internal void RegisterOverlapListener(BodyHandle bodyHandle)
        {
            contactEventHandler.RegisterListener(new CollidableReference(CollidableMobility.Dynamic, bodyHandle));
        }

        internal void UnregisterOverlapListener(StaticHandle staticHandle)
        {
            contactEventHandler.UnregisterListener(new CollidableReference(staticHandle));
        }

        internal void UnregisterOverlapListener(BodyHandle bodyHandle)
        {
            contactEventHandler.UnregisterListener(new CollidableReference(CollidableMobility.Dynamic, bodyHandle));
        }

        internal void RemoveCharacterByBodyHandle(BodyHandle bodyHandle)
        {
            characters.RemoveCharacterByBodyHandle(bodyHandle);
        }

        internal BodyHandle AddBody(in BodyDescription description)
        {
            return simulation.Bodies.Add(description);
        }

        internal BodyReference GetBodyReference(BodyHandle bodyHandle)
        {
            return new BodyReference(bodyHandle, simulation.Bodies);
        }

        internal void WakeBody(BodyHandle bodyHandle)
        {
            simulation.Awakener.AwakenBody(bodyHandle);
        }

        internal bool BodyExists(BodyHandle bodyHandle)
        {
            return simulation.Bodies.BodyExists(bodyHandle);
        }
        internal bool BodyExists(StaticHandle staticHandle)
        {
            return simulation.Statics.StaticExists(staticHandle);
        }

        internal void RemoveBody(BodyHandle bodyHandle)
        {
            simulation.Bodies.Remove(bodyHandle);
        }

        internal StaticHandle AddStatic(in StaticDescription description)
        {
            return simulation.Statics.Add(description);
        }

        internal StaticReference GetStaticReference(StaticHandle staticHandle)
        {
            return simulation.Statics.GetStaticReference(staticHandle);
        }

        internal void RemoveStatic(StaticHandle staticHandle)
        {
            simulation.Statics.Remove(staticHandle);
        }

        internal TypedIndex AddShape<TShape>(in TShape shape) where TShape : unmanaged, IShape
        {
            return simulation.Shapes.Add(shape);
        }

        internal void RemoveShape(TypedIndex index)
        {
            simulation.Shapes.Remove(index);
        }

        internal ConstraintHandle AddConstraint<TDescription>(BodyHandle bodyHandle, TDescription description) where TDescription : unmanaged, IOneBodyConstraintDescription<TDescription>
        {
            return simulation.Solver.Add(bodyHandle, description);
        }

        internal ConstraintHandle AddConstraint<TDescription>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, TDescription description) where TDescription : unmanaged, ITwoBodyConstraintDescription<TDescription>
        {
            return simulation.Solver.Add(bodyHandleA, bodyHandleB, description);
        }

        internal bool ConstraintExists(ConstraintHandle constraintHandle)
        {
            return simulation.Solver.ConstraintExists(constraintHandle);
        }

        internal void UpdateConstraint<TDescription>(ConstraintHandle constraintHandle, TDescription description) where TDescription : unmanaged, IConstraintDescription<TDescription>
        {
            simulation.Solver.ApplyDescription(constraintHandle, description);
        }

        internal void RemoveConstraint(ConstraintHandle constraintHandle)
        {
            if (simulation.Solver.ConstraintExists(constraintHandle))
                simulation.Solver.Remove(constraintHandle);
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float distance, out RayHitResult result)
        {
            var hitHandler = new SingleHitHandler();
            hitHandler.CollisionProps = collisionProps;

            simulation.RayCast(
                new System.Numerics.Vector3(origin.X, origin.Y, origin.Z),
                new System.Numerics.Vector3(direction.X, direction.Y, direction.Z),
                distance,
                ref hitHandler);

            result.Normal = hitHandler.Result.Normal;
            result.Distance = hitHandler.Result.T;
            result.Hit = hitHandler.Result.Hit;
            result.Component = default;
            result.Entity = default;
            result.Point = origin + (direction * hitHandler.Result.T);

            if (hitHandler.Result.Hit)
            {
                var tag = physicsTags[hitHandler.Result.Collidable];

                if (PhysicsProcessor.TryGetComponentById(tag.ComponentId, out var physicsComponent))
                {
                    result.Entity = physicsComponent.Entity;
                    result.Component = physicsComponent;

                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            simulation.Dispose();
            characters.Dispose();

            BufferPool.Clear();

            physicsMaterials.Dispose();
            physicsBodySettings.Dispose();
            physicsTags.Dispose();
        }
    }
}
