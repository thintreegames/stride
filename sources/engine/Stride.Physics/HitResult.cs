// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics
{
    struct RayHit
    {
        public Vector3 Normal;
        public float T;
        public CollidableReference Collidable;
        public bool Hit;
    }

    public struct RayHitResult
    {
        public Entity Entity;
        public PhysicsComponent Component;
        public Vector3 Normal;
        public Vector3 Point;
        public float Distance;
        public bool Hit;
    }

    struct MultiHitHandler : IRayHitHandler
    {
        public Buffer<RayHit> Hits;
        public CollidableProperty<CollisionProperty> CollisionProps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            var collisionProp = CollisionProps[collidable];
            return collisionProp.Camera == CollisionResponseTypes.Block || collisionProp.Visibility == CollisionResponseTypes.Block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            var collisionProp = CollisionProps[collidable];
            return collisionProp.Camera == CollisionResponseTypes.Block || collisionProp.Visibility == CollisionResponseTypes.Block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData ray, ref float maximumT, float t, in System.Numerics.Vector3 normal,
            CollidableReference collidable, int childIndex)
        {
            maximumT = t;
            ref var hit = ref Hits[ray.Id];
            if (t < hit.T)
            {
                hit.Normal = new Vector3(normal.X, normal.Y, normal.Z);
                hit.T = t;
                hit.Collidable = collidable;
                hit.Hit = true;
            }
        }
    }

    struct SingleHitHandler : IRayHitHandler
    {
        public RayHit Result;
        public CollidableProperty<CollisionProperty> CollisionProps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            var collisionProp = CollisionProps[collidable];
            return collisionProp.Camera == CollisionResponseTypes.Block || collisionProp.Visibility == CollisionResponseTypes.Block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            var collisionProp = CollisionProps[collidable];
            return collisionProp.Camera == CollisionResponseTypes.Block || collisionProp.Visibility == CollisionResponseTypes.Block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData ray, ref float maximumT, float t, in System.Numerics.Vector3 normal,
            CollidableReference collidable, int childIndex)
        {
            if (t == 0)
                return;
            if (t > maximumT)
                return;

            maximumT = t;

            Result.Normal = new Vector3(normal.X, normal.Y, normal.Z);
            Result.T = t;
            Result.Collidable = collidable;
            Result.Hit = true;
        }
    }
}
