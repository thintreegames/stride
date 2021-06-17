// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Physics
{
    public abstract class ColliderShape : IDisposable
    {
        protected const float DebugScaling = 1.0f;

        public IColliderShapeDesc Description { get; internal set; }

        internal IShape InternalShape;
        internal TypedIndex InternalShapeIndex;
        internal Entity DebugEntity;
        public Matrix DebugPrimitiveMatrix;
        internal bool IsPartOfAsset = false;

        /// <summary>
        /// The local offset
        /// </summary>
        public Vector3 LocalOffset;

        /// <summary>
        /// The local rotation
        /// </summary>
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <summary>
        /// Gets the positive center matrix.
        /// </summary>
        /// <value>
        /// The positive center matrix.
        /// </value>
        public Matrix PositiveCenterMatrix;

        /// <summary>
        /// Gets the negative center matrix.
        /// </summary>
        /// <value>
        /// The negative center matrix.
        /// </value>
        public Matrix NegativeCenterMatrix;

        protected Vector3 cachedScaling = Vector3.One;

        /// <summary>
        /// Gets or sets the scaling.
        /// Make sure that you manually created and assigned an exclusive ColliderShape to the Collider otherwise since the engine shares shapes among many Colliders, all the colliders will be scaled.
        /// Please note that this scaling has no relation to the TransformComponent scaling.
        /// </summary>
        /// <value>
        /// The scaling.
        /// </value>
        public virtual Vector3 Scaling
        {
            get
            {
                return cachedScaling;
            }
            set
            {
                var oldScale = cachedScaling;

                cachedScaling = value;
                UpdateLocalTransformations();

                //If we have a debug entity apply correct scaling to it as well
                if (DebugEntity == null) return;

                var invertedScale = Matrix.Scaling(oldScale);
                invertedScale.Invert();
                var unscaledMatrix = DebugEntity.Transform.LocalMatrix * invertedScale;
                var newScale = Matrix.Scaling(cachedScaling);
                DebugEntity.Transform.LocalMatrix = unscaledMatrix * newScale;
            }
        }

        public abstract void GetShapeInteria(float mass, out BodyInertia inertia);

        public virtual MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return null;
        }

        public virtual IDebugPrimitive CreateUpdatableDebugPrimitive(GraphicsDevice device)
        {
            return null;
        }

        public virtual void UpdateDebugPrimitive(CommandList commandList, IDebugPrimitive debugPrimitive) { }

        /// <summary>
        /// Updates the local transformations, required if you change LocalOffset and/or LocalRotation.
        /// </summary>
        public virtual void UpdateLocalTransformations()
        {
            //cache matrices used to translate the position from and to physics engine / gfx engine
            PositiveCenterMatrix = Matrix.RotationQuaternion(LocalRotation) * Matrix.Translation(LocalOffset * cachedScaling);
            Matrix.Invert(ref PositiveCenterMatrix, out NegativeCenterMatrix);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose() { }
    }
}
