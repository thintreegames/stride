// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<ColliderShapeAssetDesc>))]
    [DataContract("ColliderShapeAssetDesc")]
    [Display(50, "Asset")]
    public class ColliderShapeAssetDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The reference to the collider Shape asset.
        /// </userdoc>
        [DataMember(10)]
        public PhysicsColliderShape Shape { get; set; }

        public bool Match(object obj)
        {
            var other = obj as ColliderShapeAssetDesc;
            if (other == null) return false;

            if (other.Shape == null || Shape == null)
                return other.Shape == Shape;

            if (other.Shape.Descriptions == null || Shape.Descriptions == null)
                return other.Shape.Descriptions == Shape.Descriptions;

            if (other.Shape.Descriptions.Count != Shape.Descriptions.Count)
                return false;

            if (other.Shape.Descriptions.Where((t, i) => !t.Match(Shape.Descriptions[i])).Any())
                return false;

            // TODO: shouldn't we return true here?
            return other.Shape == Shape;
        }

        public ColliderShape CreateShape(Simulation simulation, ContentManager content)
        {
            if (Shape == null)
            {
                return null;
            }

            if (Shape.Shape == null)
            {
                Shape.Shape = PhysicsColliderShape.Compose(simulation, content, Shape.Descriptions);
            }

            return Shape.Shape;
        }
    }
}
