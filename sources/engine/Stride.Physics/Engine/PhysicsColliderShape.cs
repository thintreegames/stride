// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;

namespace Stride.Physics
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<PhysicsColliderShape>))]
    [DataSerializerGlobal(typeof(CloneSerializer<PhysicsColliderShape>), Profile = "Clone")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<PhysicsColliderShape>), Profile = "Content")]
    public class PhysicsColliderShape : IDisposable
    {
        public PhysicsColliderShape()
        {
        }

        public PhysicsColliderShape([NotNull] IEnumerable<IAssetColliderShapeDesc> descriptions)
        {
            Descriptions.AddRange(descriptions);
        }

        /// <summary>
        /// Used to serialize one or more collider shapes into one single shape
        /// Reading this value will automatically parse the Shape property into its description
        /// Writing this value will automatically compose, create and populate the Shape property
        /// </summary>
        [DataMember]
        public List<IAssetColliderShapeDesc> Descriptions { get; } = new List<IAssetColliderShapeDesc>();

        [DataMemberIgnore]
        public ColliderShape Shape { get; internal set; }

        [NotNull]
        public static PhysicsColliderShape New([NotNull] params IAssetColliderShapeDesc[] descriptions)
        {
            if (descriptions == null) throw new ArgumentNullException(nameof(descriptions));
            return new PhysicsColliderShape(descriptions);
        }

        internal static ColliderShape Compose(Simulation simulation, ContentManager content, IReadOnlyList<IAssetColliderShapeDesc> descs)
        {
            if (descs == null)
            {
                return null;
            }

            ColliderShape res = null;

            if (descs.Count == 1) //single shape case
            {
                res = CreateShape(simulation, content, descs[0]);
                if (res == null) return null;
            }

            return res;
        }

        internal static ColliderShape CreateShape(Simulation simulation, ContentManager content, IColliderShapeDesc desc)
        {
            if (desc == null)
                return null;

            ColliderShape shape = desc.CreateShape(simulation, content);

            if (shape == null) return null;

            //shape.UpdateLocalTransformations();
            shape.Description = desc;

            return shape;
        }

        public void Dispose()
        {
            if (Shape == null) return;

            Shape.Dispose();
            Shape = null;
        }
    }
}
