using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{
    public class CapsuleColliderShape : ColliderShape
    {
        public readonly float Length;
        public readonly float Radius;

        public CapsuleColliderShape(Simulation simulation, float radius, float length)
        {
            Radius = radius;
            Length = length;

            var capsule = new Capsule(radius, length);
            InternalShape = capsule;
            InternalShapeIndex = simulation.AddShape(capsule);

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(DebugScaling));
        }

        public override void GetShapeInteria(float mass, out BodyInertia inertia)
        {
            ((Capsule)InternalShape).ComputeInertia(mass, out inertia);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Capsule.New(device, Length, Radius).ToMeshDraw();
        }
    }
}
