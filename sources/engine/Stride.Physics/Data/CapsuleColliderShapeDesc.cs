// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using System;
using System.ComponentModel;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<CapsuleColliderShapeDesc>))]
    [DataContract("CapsuleColliderShapeDesc")]
    [Display(50, "Capsule")]
    public class CapsuleColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The length of the capsule (distance between the center of the two sphere centers).
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Length = 0.5f;

        /// <userdoc>
        /// The radius of the capsule.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(0.25f)]
        public float Radius = 0.25f;

        /// <userdoc>
        /// The local offset of the collider shape.
        /// </userdoc>
        [DataMember(50)]
        public Vector3 LocalOffset = Vector3.Zero;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(60)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as CapsuleColliderShapeDesc;
            return other != null &&
                   Math.Abs(other.Length - Length) < float.Epsilon &&
                   Math.Abs(other.Radius - Radius) < float.Epsilon &&
                   other.LocalOffset == LocalOffset &&
                   other.LocalRotation == LocalRotation;
        }


        public ColliderShape CreateShape(Simulation simulation, ContentManager content)
        {
            return new CapsuleColliderShape(simulation, Radius, Length)
            {
                LocalOffset = LocalOffset,
                LocalRotation = LocalRotation
            };
        }
    }
}
