using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics.Events
{
    public struct ContactAddedEvent : IPhysicsEventData
    {
        public PhysicsComponent Component { get; set; }
        public Vector3 ContactOffset { get; set; }
        public Vector3 ContactNormal { get; set; }
        public float Depth { get; set; }
    }

    public struct ContactRemovedEvent : IPhysicsEventData
    {
        public PhysicsComponent Component { get; set; }
        public Vector3 ContactOffset { get; set; }
        public Vector3 ContactNormal { get; set; }
        public float Depth { get; set; }
    }

    public struct OnColliderEnter : IPhysicsEventData
    {
        public PhysicsComponent Component { get; set; }
    }

    public struct OnColliderExit : IPhysicsEventData
    {
        public PhysicsComponent Component { get; set; }
    }
}
