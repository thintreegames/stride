using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Matrix = Stride.Core.Mathematics.Matrix;

namespace Stride.Physics
{
    public class CylinderColliderShape : ColliderShape
    {
        public readonly float Radius;
        public readonly float Length;

        public CylinderColliderShape(Simulation simulation, float radius, float length)
        {
            Radius = radius;
            Length = length;

            var cylinder = new Cylinder(radius, length);
            InternalShape = cylinder;
            InternalShapeIndex = simulation.AddShape(cylinder);

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(Radius * 2, Length, Radius * 2) * DebugScaling);
        }

        public override void GetShapeInteria(float mass, out BodyInertia inertia)
        {
            ((Cylinder)InternalShape).ComputeInertia(mass, out inertia);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Capsule.New(device).ToMeshDraw();
        }
    }
}
