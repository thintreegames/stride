using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    [DataContract("OneBodyAngularServoComponent")]
    [Display("Angular Servo Constraint")]
    public class OneBodyAngularServoComponent : PhysicsConstraintComponent
    {

        public RigidbodyComponent TargetBody { get; set; }

        private Quaternion targetOrientation;
        public Quaternion TargetOrientation
        {
            get => targetOrientation;
            set
            {
                targetOrientation = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private float servoMaxSpeed = float.MaxValue;
        public float ServoMaxSpeed
        {
            get => servoMaxSpeed;
            set
            {
                servoMaxSpeed = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private float servoBaseSpeed = 0;
        public float ServoBaseSpeed
        {
            get => servoBaseSpeed;
            set
            {
                servoBaseSpeed = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private float springFrequency = 5;
        public float SpringFrequency
        {
            get => springFrequency;
            set
            {
                springFrequency = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private float springDampingRatio = 2;
        public float SpringDampingRatio
        {
            get => springDampingRatio;
            set
            {
                springDampingRatio = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        protected override void OnAttach()
        {
            base.OnAttach();

            constraintHandle = Simulation.AddConstraint(TargetBody.BodyHandle, CreateDescription());
        }

        private OneBodyAngularServo CreateDescription()
        {
            return new OneBodyAngularServo
            {
                TargetOrientation = new System.Numerics.Quaternion(TargetOrientation.X, TargetOrientation.Y,
                                                                    TargetOrientation.Z, TargetOrientation.W),
                ServoSettings = new ServoSettings(ServoMaxSpeed, ServoBaseSpeed, 360 / TargetBody.InverseMass),
                SpringSettings = new SpringSettings(SpringFrequency, SpringDampingRatio),
            };
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            Simulation.RemoveConstraint(constraintHandle);
        }
    }
}
