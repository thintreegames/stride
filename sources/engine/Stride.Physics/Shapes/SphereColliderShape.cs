using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{
    public class SphereColliderShape : ColliderShape
    {
        public readonly float Radius;

        public SphereColliderShape(Simulation simulation, float radius)
        {
            Radius = radius;

            var sphere = new Sphere(radius);
            InternalShape = sphere;
            InternalShapeIndex = simulation.AddShape(sphere);

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(DebugScaling));
        }

        public override void GetShapeInteria(float mass, out BodyInertia inertia)
        {
            ((Capsule)InternalShape).ComputeInertia(mass, out inertia);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Sphere.New(device, Radius).ToMeshDraw();
        }
    }
}
