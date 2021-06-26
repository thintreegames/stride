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
    public class BoxColliderShape : ColliderShape
    {
        public BoxColliderShape(Simulation simulation, Vector3 size)
        {
            var box = new Box(size.X, size.Y, size.Z);
            InternalShape = box;
            InternalShapeIndex = simulation.AddShape(box);

            DebugPrimitiveMatrix = Matrix.Scaling(size * DebugScaling);
        }

        public override void GetShapeInteria(float mass, out BodyInertia inertia)
        {
            ((Cylinder)InternalShape).ComputeInertia(mass, out inertia);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cube.New(device).ToMeshDraw();
        }
    }
}
