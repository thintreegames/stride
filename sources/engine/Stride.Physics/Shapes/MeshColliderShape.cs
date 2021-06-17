using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Mesh = BepuPhysics.Collidables.Mesh;

namespace Stride.Physics
{
    public class MeshColliderShape : ColliderShape
    {
        public Vector3[] Vertices { get; private set; }
        public int[] Indices { get; private set; }

        public MeshColliderShape(Simulation simulation, Vector3[] vertices, 
            int[] indices, Vector3 scaling)
        {
            Vertices = vertices;
            Indices = indices;

            simulation.BufferPool.Take<Triangle>(indices.Length / 3, out var triangles);
            for (int i = 0; i < triangles.Length; ++i)
            {
                var i1 = indices[(i * 3) + 0];
                var i2 = indices[(i * 3) + 1];
                var i3 = indices[(i * 3) + 2];

                var vert1 = vertices[i1];
                var vert2 = vertices[i2];
                var vert3 = vertices[i3];

                triangles[i] = new Triangle(
                    new System.Numerics.Vector3(vert1.X, vert1.Y, vert1.Z),
                    new System.Numerics.Vector3(vert2.X, vert2.Y, vert2.Z),
                    new System.Numerics.Vector3(vert3.X, vert3.Y, vert3.Z));
            }

            var mesh = new Mesh(triangles,
                new System.Numerics.Vector3(scaling.X, scaling.Y, scaling.Z), simulation.BufferPool);

            InternalShape = mesh;
            InternalShapeIndex = simulation.AddShape(mesh);

            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
        }

        public override void GetShapeInteria(float mass, out BodyInertia inertia)
        {
            ((Mesh)InternalShape).ComputeClosedInertia(mass, out inertia);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            var verts = new VertexPositionNormalTexture[Vertices.Length];
            for (int i = 0; i < Vertices.Length; i++)
            {
                verts[i].Position = Vertices[i];
            }

            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, Indices, false);

            return new GeometricPrimitive(device, meshData).ToMeshDraw();
        }
    }
}
