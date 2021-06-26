using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    [DataContract("DistanceLimitConstraint")]
    [Display("Distance Limit Constraint")]
    public class DistanceLimitConstraint : PhysicsConstraintComponent
    {
        public RigidbodyComponent BodyA { get; set; }
        public RigidbodyComponent BodyB { get; set; }


        private float minimumDistance = 1;
        public float MinimumDistance
        {
            get => minimumDistance;
            set
            {
                minimumDistance = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private float maximumDistance = 0;
        public float MaximumDistance
        {
            get => maximumDistance;
            set
            {
                maximumDistance = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private Vector3 localOffsetA;
        public Vector3 LocalOffsetA
        {
            get => localOffsetA;
            set
            {
                localOffsetA = value;

                if (Simulation != null && Simulation.ConstraintExists(constraintHandle))
                {
                    Simulation.UpdateConstraint(constraintHandle, CreateDescription());
                }
            }
        }

        private Vector3 localOffsetB;
        public Vector3 LocalOffsetB
        {
            get => localOffsetB;
            set
            {
                localOffsetB = value;

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

        private BepuPhysics.Constraints.DistanceLimit CreateDescription()
        {
            return new BepuPhysics.Constraints.DistanceLimit
            {
                MinimumDistance = MinimumDistance,
                MaximumDistance = MaximumDistance,
                LocalOffsetA = new System.Numerics.Vector3(LocalOffsetA.X, LocalOffsetA.Y, LocalOffsetA.Z),
                LocalOffsetB = new System.Numerics.Vector3(LocalOffsetB.X, LocalOffsetB.Y, LocalOffsetB.Z),
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
