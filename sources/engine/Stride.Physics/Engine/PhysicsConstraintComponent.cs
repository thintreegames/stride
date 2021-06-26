using BepuPhysics;
using Stride.Core;
using Stride.Engine;

namespace Stride.Physics
{
    [DataContract("PhysicsConstraintComponent", Inherited = true)]
    [Display("PhysicsConstraint", Expand = ExpandRule.Once)]
    [ComponentCategory("Physics Constraints")]
    [AllowMultipleComponents]
    public abstract class PhysicsConstraintComponent : PhysicsComponent
    {
        protected ConstraintHandle constraintHandle;
        /// <summary>
        /// Handle of the constraint.
        /// </summary>
        [DataMemberIgnore]
        public ConstraintHandle ConstraintHandle { get { return constraintHandle; } }
    }
}
