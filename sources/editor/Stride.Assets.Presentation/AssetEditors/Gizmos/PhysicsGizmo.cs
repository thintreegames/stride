// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Engine;
using Stride.Extensions;
using Stride.Physics.Engine;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Core.Assets;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(PhysicsColliderComponent), false)]
    public class PhysicsGizmo : EntityGizmo<PhysicsColliderComponent>
    {
        private readonly List<Entity> spawnedEntities = new List<Entity>();

        private bool rendering;

        public PhysicsGizmo(EntityComponent component)
            : base(component)
        {
        }
        public PhysicsGizmo(PhysicsColliderComponent component) : base(component)
        {
            RenderGroup = PhysicsShapesGroup;
        }

        protected override Entity Create()
        {
            return new Entity("Physics Gizmo Root Entity (id={0})".ToFormat(ContentEntity.Id));
        }

        protected override void Destroy()
        {
            foreach (var spawnedEntity in spawnedEntities)
            {
                EditorScene.Entities.Remove(spawnedEntity);
            }
        }

        private struct PhysicsElementInfo
        {
            private readonly ColliderShape shape;
            private readonly bool isKinematic;
            private readonly bool isTrigger;
            private readonly List<IInlineColliderShapeDesc> colliderShapes;

            public PhysicsElementInfo(PhysicsColliderComponent component)
            {
                shape = component.ColliderShape;
                var rigidbodyComponent = component as RigidbodyComponent;
                isKinematic = rigidbodyComponent != null && rigidbodyComponent.IsKinematic;
                colliderShapes = component.ColliderShapes != null ? CloneDescs(component.ColliderShapes) : null;

                var triggerBase = component as StaticColliderComponent;
                isTrigger = triggerBase != null && triggerBase.GenerateOverlapEvents;
            }

            public bool HasChanged(PhysicsColliderComponent component)
            {
                var triggerBase = component as StaticColliderComponent;
                var rb = component as RigidbodyComponent;

                return shape != component.ColliderShape ||
                (colliderShapes == null && component.ColliderShapes != null) ||
                (colliderShapes != null && component.ColliderShapes == null) ||
                DescsAreDifferent(colliderShapes, component.ColliderShapes) ||
                component.ColliderShapeChanged ||
                (rb != null && isKinematic != rb.IsKinematic) ||
                triggerBase != null && triggerBase.GenerateOverlapEvents != isTrigger ||
                shape != null && component.DebugEntity == null;
            }

            private static List<IInlineColliderShapeDesc> CloneDescs(IEnumerable<IInlineColliderShapeDesc> descs)
            {
                var res = new List<IInlineColliderShapeDesc>();
                foreach (var desc in descs)
                {
                    if (desc == null)
                    {
                        res.Add(null);
                    }
                    else
                    {
                        var cloned = AssetCloner.Clone(desc, AssetClonerFlags.KeepReferences);
                        res.Add(cloned);
                    }
                }
                return res;
            }
        }

        private readonly Dictionary<PhysicsColliderComponent, PhysicsElementInfo> elementToEntity = new Dictionary<PhysicsColliderComponent, PhysicsElementInfo>();

        private static bool DescsAreDifferent(IList<IInlineColliderShapeDesc> left, IList<IInlineColliderShapeDesc> right)
        {
            if (left == null && right != null || right == null && left != null) return true;

            if (left == null) return false;

            if (left.Count != right.Count) return true;

            for (var i = 0; i < left.Count; i++)
            {
                var leftDesc = left[i];
                var rightDesc = right[i];
                if (leftDesc != null && !leftDesc.Match(rightDesc)) return true;
            }

            return false;
        }

        public override void Update()
        {
            if (ContentEntity == null)
                return;

            if ((!IsEnabled || !Component.Enabled) && Component.DebugEntity != null)
            {
                if (!rendering) return;

                Component.DebugEntity.Enable<ModelComponent>(false, true);
                rendering = false;
                return;
            }

            // Create and add the element missing
            PhysicsElementInfo entityInfo;

            var modelComponent = ContentEntity.Get<ModelComponent>();

            if (!elementToEntity.TryGetValue(Component, out entityInfo) || entityInfo.HasChanged(Component))
            {
                //remove and clean up the old debug entity
                if (Component.DebugEntity != null)
                {
                    spawnedEntities.Remove(Component.DebugEntity);
                    Component.RemoveDebugEntity(EditorScene);
                    Component.DebugEntity = null;
                }

                //compose shape and fill data as data is not being filled by the processor when we run from the editor
                Component.ComposeShape();

                Component.AddDebugEntity(EditorScene, RenderGroup, true);
                if (Component.DebugEntity != null)
                {
                    spawnedEntities.Add(Component.DebugEntity);
                    Component.DebugEntity?.Enable<ModelComponent>(false, true);
                    rendering = false; //make sure we refresh enabled flags?
                }

                elementToEntity[Component] = new PhysicsElementInfo(Component);
            }

            if (Component.DebugEntity != null)
            {
                if (IsEnabled && Component.Enabled && !rendering)
                {
                    Component.DebugEntity?.Enable<ModelComponent>(true, true);
                    rendering = true;
                }

                Vector3 pos;
                Quaternion rot;
                ContentEntity.Transform.WorldMatrix.Decompose(out _, out rot, out pos);
                Component.DebugEntity.Transform.Position = pos;
                Component.DebugEntity.Transform.Rotation = rot;
            }

            GizmoRootEntity.Transform.LocalMatrix = ContentEntity.Transform.WorldMatrix;
            GizmoRootEntity.Transform.UseTRS = false;
        }
    }
}
