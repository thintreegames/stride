using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics
{
    [DataContract("RigidbodyComponent")]
    [Display("Rigidbody")]
    public sealed class RigidbodyComponent : PhysicsColliderComponent
    {
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private bool passedFirstPosInterpolateFrame, passedFirstRotInterpoalteFrame;

        private BodyHandle bodyHandle;
        /// <summary>
        /// Handle of the body associated with the rigidbody.
        /// </summary>
        [DataMemberIgnore]
        public BodyHandle BodyHandle { get { return bodyHandle; } }

        private float mass = 1f;

        public float InverseMass
        { 
            get
            {
                return GetBodyReference().LocalInertia.InverseMass;
            }
        }

        public float Mass
        {
            get
            {
                return mass;
            }
            set
            {
                mass = value;

                if (Simulation != null && Simulation.BodyExists(bodyHandle))
                {
                    colliderShape.GetShapeInteria(value, out var interia);
                    Simulation.GetBodyReference(bodyHandle).SetLocalInertia(interia);
                }
            }
        }

        private bool isKinematic = false;

        public bool IsKinematic
        {
            get => isKinematic;
            set
            {
                isKinematic = value;

                if (Simulation != null && Simulation.BodyExists(bodyHandle))
                {
                    if (value)
                    {
                        Simulation.GetBodyReference(bodyHandle).SetLocalInertia(default);
                    }
                    else
                    {
                        colliderShape.GetShapeInteria(Mass, out var interia);
                        Simulation.GetBodyReference(bodyHandle).SetLocalInertia(interia);
                    }
                }
            }
        }

        public bool Interpolate { get; set; } = false;

        public float SpeculativeMargin { get; set; }

        public float SleepThreshold { get; set; } = 0.01f;

        public ContinuousDetectionMode DetectionMode { get; set; }

        public BodyReference GetBodyReference()
        {
            return Simulation.GetBodyReference(BodyHandle);
        }

        protected override void OnAttach()
        {
            base.OnAttach();

            Entity.Transform.UpdateWorldMatrix();
            Entity.Transform.GetWorldTransformation(out var entityPosition, out var entityRotation, out var scale);

            var position = new System.Numerics.Vector3(entityPosition.X, entityPosition.Y, entityPosition.Z);
            var rotation = new System.Numerics.Quaternion(entityRotation.X, entityRotation.Y,
                entityRotation.Z, entityRotation.W);

            var activityDescription = new BodyActivityDescription(SleepThreshold);

            BodyInertia interia = default;

            if (!IsKinematic)
            {
                ColliderShape.GetShapeInteria(Mass, out interia);
            }

            // Hardcoded default SweepConvergenceThreshold and MinimumSweepTimestep values
            // in future perhaps allow user to define this values
            var continuity = new ContinuousDetectionSettings
            {
                Mode = DetectionMode,
                SweepConvergenceThreshold = DetectionMode == ContinuousDetectionMode.Continuous ? 1e-3f : 0,
                MinimumSweepTimestep = DetectionMode == ContinuousDetectionMode.Continuous ? 1e-3f : 0
            };

            var collidableDescription = new CollidableDescription(ColliderShape.InternalShapeIndex, SpeculativeMargin,
                continuity);

            var rigidPose = new RigidPose(position, rotation);

            var bodyDescription = BodyDescription.CreateDynamic(rigidPose, interia, collidableDescription, 
                activityDescription);

            bodyHandle = Simulation.AddBody(bodyDescription);

            Simulation.physicsBodySettings.Allocate(bodyHandle) = new SimplePhysicsBodySettings
            {
                CustomDamping = false,
                CustomGravity = false,
            };

            Simulation.physicsMaterials.Allocate(bodyHandle) = new SimplePhysicsMaterial
            {
                FrictionCoefficient = 1f,
                MaximumRecoveryVelocity = 2f,
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1)
            };

            Simulation.physicsTags.Allocate(bodyHandle) = new SimplePhysicsTag
            {
                ComponentId = Id
            };

            Simulation.collisionProps.Allocate(bodyHandle) = new CollisionProperty
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
                Simulation.RegisterOverlapListener(BodyHandle);
            }
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            if (GenerateOverlapEvents)
            {
                Simulation.UnregisterOverlapListener(BodyHandle);
            }

            Simulation.RemoveBody(bodyHandle);
        }

        public void SetPosition(Vector3 newPosition)
        {
            var bodyReference = GetBodyReference();
            bodyReference.Pose.Position = new System.Numerics.Vector3(newPosition.X, newPosition.Y, newPosition.Z);
        }

        public void SetRotation(Quaternion newRotation)
        {
            GetBodyReference().Pose.Orientation =
                new System.Numerics.Quaternion(newRotation.X, newRotation.Y, newRotation.Z, newRotation.W);
        }

        public Vector3 GetPosition()
        {
            var physicsPosition = GetBodyReference().Pose.Position;
            return new Vector3(physicsPosition.X, physicsPosition.Y, physicsPosition.Z);
        }

        public Vector3 GetInterpolatedPosition()
        {
            var physicsPosition = GetBodyReference().Pose.Position;

            if (!passedFirstPosInterpolateFrame)
            {
                previousPosition = new Vector3(physicsPosition.X, physicsPosition.Y, physicsPosition.Z);
                passedFirstPosInterpolateFrame = true;
            }

            var currentPosition = new Vector3(physicsPosition.X, physicsPosition.Y, physicsPosition.Z);

            var lerpedPosition = Vector3.Lerp(previousPosition, currentPosition, 0.5f);

            previousPosition = new Vector3(physicsPosition.X, physicsPosition.Y, physicsPosition.Z);

            return lerpedPosition;
        }

        public Quaternion GetRotation()
        {
            var physicsRotation = GetBodyReference().Pose.Orientation;
            return new Quaternion(physicsRotation.X, physicsRotation.Y, physicsRotation.Z, physicsRotation.W);
        }

        public Quaternion GetInterpolatedRotation()
        {
            var physicsRotation = GetBodyReference().Pose.Orientation;

            if (!passedFirstRotInterpoalteFrame)
            {
                previousRotation = new Quaternion(physicsRotation.X, physicsRotation.Y, physicsRotation.Z, physicsRotation.W);
                passedFirstRotInterpoalteFrame = true;
            }

            var currentRotation = new Quaternion(physicsRotation.X, physicsRotation.Y, physicsRotation.Z, physicsRotation.W);

            var lerpedRotation = Quaternion.Lerp(previousRotation, currentRotation, 0.5f);

            previousRotation = new Quaternion(physicsRotation.X, physicsRotation.Y, physicsRotation.Z, physicsRotation.W);

            return lerpedRotation;
        }

        public void ApplyLinearImpulse(Vector3 force)
        {
            var bodyReference = GetBodyReference();
            Simulation.WakeBody(bodyHandle);

            var numericsForce = new System.Numerics.Vector3(force.X, force.Y, force.Z);
            bodyReference.ApplyLinearImpulse(numericsForce);
        }

        public void SetGravity(Vector3 force)
        {
            ref var physicsBodySettings = ref Simulation.physicsBodySettings[bodyHandle];
            physicsBodySettings.CustomGravity = true;
            physicsBodySettings.Gravity = new System.Numerics.Vector3(force.X, force.Y, force.Z);
        }

        public void DefaultGravity()
        {
            ref var physicsBodySettings = ref Simulation.physicsBodySettings[bodyHandle];
            physicsBodySettings.CustomGravity = false;
        }

        public void Wake()
        {
            Simulation.WakeBody(bodyHandle);
        }

        protected override void OnChangeShape(ColliderShape newShape)
        {
            base.OnChangeShape(newShape);

            if (Simulation != null && Simulation.BodyExists(bodyHandle))
            {
                newShape.GetShapeInteria(Mass, out var interia);

                var bodyReference = Simulation.GetBodyReference(BodyHandle);

                bodyReference.SetLocalInertia(interia);
                bodyReference.SetShape(newShape.InternalShapeIndex);
            }
        }
    }
}
