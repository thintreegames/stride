using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core;
using Stride.Engine;

namespace Stride.Physics
{
    [DataContract("StaticColliderComponent")]
    [Display("Static Collider")]
    public sealed class StaticColliderComponent : PhysicsColliderComponent
    {
        [DataMember(100)]
        public bool AlwaysUpdateNaviMeshCache { get; set; } = false;

        private StaticHandle staticHandle;
        public StaticHandle StaticHandle { get { return staticHandle; } }

        /*
            /// <summary>
            /// Gets or sets if this element is enabled in the physics engine
            /// </summary>
            /// <value>
            /// true, false
            /// </value>
            /// <userdoc>
            /// If this element is enabled in the physics engine
            /// </userdoc>
            [DataMember(-10)]
            [DefaultValue(true)]
            public override bool Enabled
            {
                get
                {
                    return base.Enabled;
                }
                set
                {
                    base.Enabled = value;

                    if (NativeCollisionObject == null) return;

                    if (value && isTrigger)
                    {
                        //We still have to add this flag if we are actively a trigger
                        NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.NoContactResponse;
                    }
                }
            }*/

        public float SpeculativeMargin { get; set; }

        protected override void OnAttach()
        {

            //this will set all the properties in the native side object
            base.OnAttach();

            Entity.Transform.UpdateWorldMatrix();
            Entity.Transform.GetWorldTransformation(out var entityPosition, out var entityRotation, out var scale);

            var position = new System.Numerics.Vector3(entityPosition.X, entityPosition.Y, entityPosition.Z);
            var rotation = new System.Numerics.Quaternion(entityRotation.X, entityRotation.Y,
                entityRotation.Z, entityRotation.W);

            var collidableDescription = new CollidableDescription(ColliderShape.InternalShapeIndex, SpeculativeMargin);

            staticHandle = Simulation.AddStatic(new StaticDescription(position, rotation, collidableDescription));

            Simulation.physicsBodySettings.Allocate(staticHandle) = new SimplePhysicsBodySettings
            {
                CustomDamping = false,
                CustomGravity = false,
            };

            Simulation.physicsMaterials.Allocate(staticHandle) = new SimplePhysicsMaterial
            {
                FrictionCoefficient = 1f,
                MaximumRecoveryVelocity = 2f,
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1)
            };

            Simulation.physicsTags.Allocate(staticHandle) = new SimplePhysicsTag
            {
                ComponentId = Id
            };

            Simulation.collisionProps.Allocate(staticHandle) = new CollisionProperty
            {
                GenerateOverlapEvents = GenerateOverlapEvents,

                CollisionEnabled = CollisionPresets.CollisionEnabled,
                ObjectType = CollisionPresets.ObjectType,

                Visibility = CollisionPresets.CollisionResponses.TraceResponse.Visibility,
                Camera = CollisionPresets.CollisionResponses.TraceResponse.Camera,

                WorldStatic = CollisionPresets.CollisionResponses.ObjectResponse.WorldStatic,
                WorldDynamic = CollisionPresets.CollisionResponses.ObjectResponse.WorldDynamic,
                Pawn = CollisionPresets.CollisionResponses.ObjectResponse.Pawn,
                PhysicsBody = CollisionPresets.CollisionResponses.ObjectResponse.PhysicsBody,
            };

            if (GenerateOverlapEvents)
            {
                Simulation.RegisterOverlapListener(staticHandle);
            }
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            if (GenerateOverlapEvents)
            {
                Simulation.UnregisterOverlapListener(staticHandle);
            }

            Simulation.RemoveStatic(staticHandle);
        }

        protected override void OnChangeShape(ColliderShape newShape)
        {
            base.OnChangeShape(newShape);


            if (Simulation != null && Simulation.BodyExists(StaticHandle))
            {
                var staticReference = Simulation.GetStaticReference(StaticHandle);
                staticReference.SetShape(newShape.InternalShapeIndex);
            }
        }
    }
}
