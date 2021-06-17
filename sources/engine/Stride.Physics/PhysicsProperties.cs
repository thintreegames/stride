using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;

namespace Stride.Physics
{
    public struct SimplePhysicsMaterial
    {
        public SpringSettings SpringSettings;
        public float FrictionCoefficient;
        public float MaximumRecoveryVelocity;
    }

    public struct SimplePhysicsBodySettings
    {
        public bool CustomDamping;
        public float LinearDamping;
        public float AngularDamping;

        public bool CustomGravity;
        public Vector3 Gravity;
    }

    public struct SimplePhysicsTag
    {
        public Guid ComponentId;
    }

    public struct CollisionProperty
    {
        public bool GenerateOverlapEvents;

        public CollisionType CollisionEnabled;
        public CollisionObjectType ObjectType;

        public CollisionResponseTypes Visibility;
        public CollisionResponseTypes Camera;

        public CollisionResponseTypes WorldStatic;
        public CollisionResponseTypes WorldDynamic;
        public CollisionResponseTypes Pawn;
        public CollisionResponseTypes PhysicsBody;
    }
}
