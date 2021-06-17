using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Physics
{
    [DataContract("PhysicsColliderComponent")]
    [Display("PhysicsColliderComponent")]
    public abstract class PhysicsColliderComponent : PhysicsComponent
    {
        /// <userdoc>
        /// The reference to the collider shape of this element.
        /// </userdoc>
        [DataMember(200)]
        [Category]
        [MemberCollection(NotNullItems = true)]
        public ColliderShapeCollection ColliderShapes { get; }

        public bool GenerateOverlapEvents { get; set; }

        public ICollisionPreset CollisionPresets { get; set; } = new CollisionPresetDefault();

        [DataMemberIgnore]
        protected ColliderShape colliderShape;


        [DataMemberIgnore]
        public virtual ColliderShape ColliderShape
        {
            get
            {
                return colliderShape;
            }
            set
            {
                if (value == null)
                    return;

                colliderShape = value;

                if (Enabled)
                {
                    OnChangeShape(value);
                }
            }
        }

        [DataMemberIgnore]
        public bool ColliderShapeChanged { get; private set; }

        protected PhysicsColliderComponent()
        {
            ColliderShapes = new ColliderShapeCollection(this);
        }

        public void AddDebugEntity(Scene scene, RenderGroup renderGroup = RenderGroup.Group0, bool alwaysAddOffset = false)
        {
            if (DebugEntity != null) return;

            var entity = Data?.PhysicsComponent?.DebugShapeRendering?.CreateDebugEntity(this, renderGroup, alwaysAddOffset);
            DebugEntity = entity;

            if (DebugEntity == null) return;

            scene.Entities.Add(entity);
        }

        public void RemoveDebugEntity(Scene scene)
        {
            if (DebugEntity == null) return;

            scene.Entities.Remove(DebugEntity);
            DebugEntity = null;
        }


        public void ComposeShape()
        {
            if (Simulation == null || Simulation.PhysicsProcessor == null) return;

            ColliderShapeChanged = false;

            if (ColliderShapes.Count == 1) //single shape case
            {
                if (ColliderShapes[0] == null) return;

                var content = Simulation.PhysicsProcessor.Services.GetService<ContentManager>();

                colliderShape = PhysicsColliderShape.CreateShape(Simulation, content, ColliderShapes[0]);
            }
        }

        protected override void OnAttach()
        {
            if (ColliderShapes.Count == 0 && ColliderShape == null)
            {
                logger.Error($"Entity {Entity.Name} has a BepuPhysicsComponent without any collider shape.");
                return; //no shape no purpose
            }
            else if (ColliderShape == null)
            {
                ComposeShape();
                if (ColliderShape == null)
                {
                    logger.Error($"Entity {Entity.Name}'s BepuPhysicsComponent failed to compose its collider shape.");
                    return; //no shape no purpose
                }
            }
        }

        protected virtual void OnChangeShape(ColliderShape shape)
        {

        }

        [DataContract]
        public class ColliderShapeCollection : FastCollection<IInlineColliderShapeDesc>
        {
            private PhysicsColliderComponent component;

            public ColliderShapeCollection(PhysicsColliderComponent componentParam)
            {
                component = componentParam;
            }

            /// <inheritdoc/>
            protected override void InsertItem(int index, IInlineColliderShapeDesc item)
            {
                base.InsertItem(index, item);
                component.ColliderShapeChanged = true;
            }

            /// <inheritdoc/>
            protected override void RemoveItem(int index)
            {
                base.RemoveItem(index);
                component.ColliderShapeChanged = true;
            }

            /// <inheritdoc/>
            protected override void ClearItems()
            {
                base.ClearItems();
                component.ColliderShapeChanged = true;
            }

            /// <inheritdoc/>
            protected override void SetItem(int index, IInlineColliderShapeDesc item)
            {
                base.SetItem(index, item);
                component.ColliderShapeChanged = true;
            }
        }
    }
}
