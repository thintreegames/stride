using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Engine;

namespace Stride.Physics.Events
{
    public interface IPhysicsEventData
    {
        PhysicsComponent Component { get; set; }
    }
}
