// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using BepuPhysics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics
{
    [DataContract("CharacterControllerComponent")]
    [Display("Character Controller")]
    [RequireComponent(typeof(RigidbodyComponent))]
    public class CharacterControllerComponent : PhysicsComponent
    {
        /// <summary>
        /// Direction the character is looking in world space. Defines the forward direction for movement.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 ViewDirection;
        /// <summary>
        /// Target horizontal velocity. 
        /// X component refers to desired velocity along the strafing direction (perpendicular to the view direction projected down to the surface), 
        /// Y component refers to the desired velocity along the forward direction (aligned with the view direction projected down to the surface).
        /// </summary>
        [DataMemberIgnore]
        public Vector2 TargetVelocity;

        /// <summary>
        /// Character's up direction in the local space of the character's body.
        /// </summary>
        public Vector3 LocalUp { get; set; } = Vector3.UnitY;
        /// <summary>
        /// Velocity at which the character pushes off the support during a jump.
        /// </summary>
        public float JumpVelocity { get; set; } = 6;
        /// <summary>
        /// Maximum force the character can apply tangent to the supporting surface to move.
        /// </summary>
        public float MaximumHorizontalForce { get; set; } = 20;
        /// <summary>
        /// Maximum force the character can apply to glue itself to the supporting surface.
        /// </summary>
        public float MaximumVerticalForce { get; set; } = 100;
        /// <summary>
        /// Cosine of the maximum slope angle that the character can treat as a support.
        /// </summary>
        public float MaximumSlope { get; set; } = MathF.PI * 0.25f;
        /// <summary>
        /// Depth threshold beyond which a contact is considered a support if it the normal allows it.
        /// </summary>
        public float MinimumSupportDepth { get; set; } = -0.0035f;


        private float mass = 1f;

        public float Mass
        {
            get
            {
                return mass;
            }
            set
            {
                mass = value;

                if (Simulation != null && Simulation.BodyExists(rigidBody.BodyHandle))
                {
                    Simulation.GetBodyReference(rigidBody.BodyHandle).SetLocalInertia(new BodyInertia { InverseMass = 1f / Mass });
                }
            }
        }

        private RigidbodyComponent rigidBody;

        protected override void OnAttach()
        {
            base.OnAttach();

            rigidBody = Entity.Get<RigidbodyComponent>();

            BodyInertia interia = new BodyInertia { InverseMass = 1f / Mass };
            rigidBody.GetBodyReference().SetLocalInertia(interia);

            ref var character = ref Simulation.AllocateCharacter(rigidBody.BodyHandle);
            character.LocalUp = new System.Numerics.Vector3(LocalUp.X, LocalUp.Y, LocalUp.Z);
            character.CosMaximumSlope = MathF.Cos(MaximumSlope);
            character.JumpVelocity = JumpVelocity;
            character.MaximumVerticalForce = MaximumVerticalForce;
            character.MaximumHorizontalForce = MaximumHorizontalForce;
            character.MinimumSupportDepth = MinimumSupportDepth;
            character.MinimumSupportContinuationDepth = -rigidBody.SpeculativeMargin;

            Simulation.physicsBodySettings.Allocate(rigidBody.BodyHandle) = new SimplePhysicsBodySettings
            {
                CustomDamping = false,
                CustomGravity = false,
            };

            Simulation.physicsMaterials.Allocate(rigidBody.BodyHandle) = new SimplePhysicsMaterial
            {
                FrictionCoefficient = 1f,
                MaximumRecoveryVelocity = 2f,
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1)
            };

            Simulation.physicsTags.Allocate(rigidBody.BodyHandle) = new SimplePhysicsTag
            {
                ComponentId = Id
            };
        }

        public void Teleport(Vector3 position)
        {
            var characterBody = Simulation.GetBodyReference(rigidBody.BodyHandle);
            characterBody.Pose.Position = new System.Numerics.Vector3(position.X, position.Y, position.Z);
        }

        public void ResetInteria()
        {
            BodyInertia interia = new BodyInertia { InverseMass = 1f / Mass };
            rigidBody.GetBodyReference().SetLocalInertia(interia);
        }

        public void Move(Vector2 newMoveDirection, Vector3 newViewDirection)
        {
            var bepuTargetVelocity = new System.Numerics.Vector2(newMoveDirection.X, newMoveDirection.Y);
            var bepuViewDirection = new System.Numerics.Vector3(newViewDirection.X, newViewDirection.Y, newViewDirection.Z);

            ref var character = ref Simulation.GetCharacterByBodyHandle(rigidBody.BodyHandle);

            var characterBody = Simulation.GetBodyReference(rigidBody.BodyHandle);
            //Modifying the character's raw data does not automatically wake the character up, so we do so explicitly if necessary.
            //If you don't explicitly wake the character up, it won't respond to the changed motion goals.
            //(You can also specify a negative deactivation threshold in the BodyActivityDescription to prevent the character from sleeping at all.)
            if (!characterBody.Awake &&
                ((character.TryJump && character.Supported) ||
                bepuTargetVelocity != character.TargetVelocity ||
                (newMoveDirection != Vector2.Zero && character.ViewDirection != bepuViewDirection)))
            {
                Simulation.WakeBody(rigidBody.BodyHandle);
            }

            character.TargetVelocity = bepuTargetVelocity;
            character.ViewDirection = bepuViewDirection;
        }

        public void Jump()
        {
            ref var character = ref Simulation.GetCharacterByBodyHandle(rigidBody.BodyHandle);
            character.TryJump = true;
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            Simulation.RemoveCharacterByBodyHandle(rigidBody.BodyHandle);
        }
    }
}
