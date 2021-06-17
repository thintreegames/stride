// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<BoxColliderShapeDesc>))]
    [DataContract("BoxColliderShapeDesc")]
    [Display(50, "Box")]
    public class BoxColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The size of one edge of the box.
        /// </userdoc>
        [DataMember(5)]
        public Vector3 Size = Vector3.One;

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
            var other = obj as BoxColliderShapeDesc;
            return other?.Size == Size && other.LocalOffset == LocalOffset && other.LocalRotation == LocalRotation;
        }

        public ColliderShape CreateShape(Simulation simulation, ContentManager content)
        {
            return new BoxColliderShape(simulation, Size)
            {
                LocalOffset = LocalOffset,
                LocalRotation = LocalRotation
            };
        }
    }
}
