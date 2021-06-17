// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<SphereColliderShapeDesc>))]
    [DataContract("SphereColliderShapeDesc")]
    [Display(50, "Sphere")]
    public class SphereColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The radius of the sphere.
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
            var other = obj as SphereColliderShapeDesc;
            return other != null &&
                   Math.Abs(other.Radius - Radius) < float.Epsilon &&
                   other.LocalOffset == LocalOffset &&
                   other.LocalRotation == LocalRotation;
        }


        public ColliderShape CreateShape(Simulation simulation, ContentManager content)
        {
            return new SphereColliderShape(simulation, Radius)
            {
                LocalOffset = LocalOffset,
                LocalRotation = LocalRotation
            };
        }
    }
}
