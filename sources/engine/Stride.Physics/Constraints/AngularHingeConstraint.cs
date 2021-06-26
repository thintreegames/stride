using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    [DataContract("AngularHingeConstraint")]
    [Display("Angular Hinge Constraint")]
    public class AngularHingeConstraint : PhysicsConstraintComponent
    {
        public RigidbodyComponent BodyA { get; set; }
        public RigidbodyComponent BodyB { get; set; }

        private Vector3 localHingeAxisA;
        public Vector3 LocalHingeAxisA
        {
            get => localHingeAxisA;
            set
            {
                localHingeAxisA = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private Vector3 localHingeAxisB;
        public Vector3 LocalHingeAxisB
        {
            get => localHingeAxisB;
            set
            {
                localHingeAxisB = value;

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

            constraintHandle = Simulation.AddConstraint(BodyA.BodyHandle, BodyB.BodyHandle, CreateDescription());
        }

        private BepuPhysics.Constraints.AngularHinge CreateDescription()
        {
            return new BepuPhysics.Constraints.AngularHinge()
            {
                LocalHingeAxisA = new System.Numerics.Vector3(LocalHingeAxisA.X, LocalHingeAxisA.Y, LocalHingeAxisA.Z),
                LocalHingeAxisB = new System.Numerics.Vector3(LocalHingeAxisB.X, LocalHingeAxisB.Y, LocalHingeAxisB.Z),
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(SpringFrequency, SpringDampingRatio),
            };
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            Simulation.RemoveConstraint(constraintHandle);
        }
    }
}
