// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<CylinderColliderShapeDesc>))]
    [DataContract("CylinderColliderShapeDesc")]
    [Display(50, "Cylinder")]
    public class CylinderColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Radius of the cylinder
        /// </userdoc>
        [DataMember(5)]
        public float Radius = 0.25f;

        /// <summary>
        /// Length of the cylinder
        /// </summary>
        [DataMember(10)]
        public float Length = 1f;

        /// <userdoc>
        /// The local offset of the collider shape.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 LocalOffset = Vector3.Zero;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as CylinderColliderShape;
            return other?.Radius == Radius 
                && other.Length == Length 
                && other.LocalOffset == LocalOffset 
                && other.LocalRotation == LocalRotation;
        }

        public ColliderShape CreateShape(Simulation simulation, ContentManager content)
        {
            return new CylinderColliderShape(simulation, Radius, Length)
            {
                LocalOffset = LocalOffset,
                LocalRotation = LocalRotation
            };
        }
    }
}
