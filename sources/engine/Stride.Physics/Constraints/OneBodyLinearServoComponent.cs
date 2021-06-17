using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    [DataContract("OneBodyLinearServoComponent")]
    [Display("Linear Servo Constraint")]
    public class OneBodyLinearServoComponent : PhysicsConstraintComponent
    {
        public RigidbodyComponent TargetBody { get; set; }

        private Vector3 targetPosition;
        public Vector3 TargetPosition
        {
            get => targetPosition;
            set
            {
                targetPosition = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private Vector3 localOffset;
        public Vector3 LocalOffset
        {
            get => localOffset;
            set
            {
                localOffset = value;

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

        private OneBodyLinearServo CreateDescription()
        {
            return new OneBodyLinearServo
            {
                LocalOffset = new System.Numerics.Vector3(LocalOffset.X, LocalOffset.Y, LocalOffset.Z),
                Target = new System.Numerics.Vector3(TargetPosition.X, TargetPosition.Y, TargetPosition.Z),
                ServoSettings = new ServoSettings(ServoMaxSpeed, ServoBaseSpeed, 360 / TargetBody.InverseMass),
                SpringSettings = new SpringSettings(SpringFrequency, SpringDampingRatio)
            };
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            Simulation.RemoveConstraint(constraintHandle);
        }
    }
}
