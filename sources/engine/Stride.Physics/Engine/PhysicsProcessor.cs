// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;
using Stride.Physics.Engine;
using Stride.Rendering;

namespace Stride.Physics
{
    public class PhysicsProcessor : EntityProcessor<PhysicsComponent, PhysicsProcessor.AssociatedData>
    {
        public class AssociatedData
        {
            public PhysicsComponent PhysicsComponent;
            public TransformComponent TransformComponent;
            public ModelComponent ModelComponent; //not mandatory, could be null e.g. invisible triggers
        }

        private readonly Dictionary<Guid, PhysicsComponent> elementIdToElement = new Dictionary<Guid, PhysicsComponent>();
        private readonly List<PhysicsComponent> elements = new List<PhysicsComponent>();
        private readonly List<RigidbodyComponent> rigidElements = new List<RigidbodyComponent>();
        private readonly List<PhysicsColliderComponent> colliders = new List<PhysicsColliderComponent>();

        private readonly HashSet<PhysicsComponent> currentFrameRemovals = new HashSet<PhysicsComponent>();

        private Bepu2PhysicsSystem physicsSystem;

        private bool colliderShapesRendering;
        private PhysicsShapesRenderingService debugShapeRendering;

        private Scene parentScene;
        private Scene debugScene;

        private RenderGroup colliderShapesRenderGroup { get; set; } = RenderGroup.Group0;


        public Simulation Simulation { get; private set; }

        /// <summary>
        /// Gets or sets the associated parent scene to render the physics debug shapes. Assigned with default one on <see cref="OnSystemAdd"/>
        /// </summary>
        /// <value>
        /// The parent scene.
        /// </value>
        public Scene ParentScene
        {
            get => parentScene;
            set
            {
                if (value != parentScene)
                {
                    if (parentScene != null && debugShapeRendering.Enabled)
                    {
                        // If debug rendering is running, disable it and re-enable for new scene system
                        RenderColliderShapes(false);
                        parentScene = value;
                        RenderColliderShapes(true);
                    }
                    else
                    {
                        parentScene = value;
                    }
                }
            }
        }


        public PhysicsProcessor()
            : base(typeof(TransformComponent))
        {
            Order = 0xFFFF;
        }

        internal void RenderColliderShapes(bool enabled)
        {
            debugShapeRendering.Enabled = enabled;

            colliderShapesRendering = enabled;

            if (!colliderShapesRendering)
            {
                if (debugScene != null)
                {
                    debugScene.Dispose();

                    foreach (var element in elements)
                    {
                        if (element is PhysicsColliderComponent colliderComponent)
                        {
                            colliderComponent.RemoveDebugEntity(debugScene);
                        }
                    }

                    // Remove from parent scene
                    debugScene.Parent = null;
                }
            }
            else
            {
                debugScene = new Scene();

                foreach (var element in elements)
                {
                    if (element.Enabled)
                    {
                        if (element is PhysicsColliderComponent colliderComponent)
                        {
                            colliderComponent.AddDebugEntity(debugScene, colliderShapesRenderGroup);
                        }
                    }
                }

                debugScene.Parent = parentScene;
            }
        }


        protected override AssociatedData GenerateComponentData(Entity entity, PhysicsComponent component)
        {
            var data = new AssociatedData
            {
                PhysicsComponent = component,
                TransformComponent = entity.Transform,
                ModelComponent = entity.Get<ModelComponent>(),
            };

            data.PhysicsComponent.Simulation = Simulation;
            data.PhysicsComponent.DebugShapeRendering = debugShapeRendering;

            return data;
        }

        protected override bool IsAssociatedDataValid(Entity entity, PhysicsComponent physicsComponent, AssociatedData associatedData)
        {
            return physicsComponent == associatedData.PhysicsComponent &&
                entity.Transform == associatedData.TransformComponent &&
                entity.Get<ModelComponent>() == associatedData.ModelComponent;
        }


        protected override void OnEntityComponentAdding(Entity entity, PhysicsComponent component, AssociatedData data)
        {
            // Tagged for removal? If yes, cancel it
            if (currentFrameRemovals.Remove(component))
                return;

            component.Attach(data);

            if (component is PhysicsColliderComponent colliderComponent)
            {
                colliders.Add(colliderComponent);

                if (colliderShapesRendering)
                {
                    colliderComponent.AddDebugEntity(debugScene, colliderShapesRenderGroup);
                }
            }

            elements.Add(component);
            elementIdToElement.Add(component.Id, component);

            if (component is RigidbodyComponent rigidbodyComponent)
            {
                rigidElements.Add(rigidbodyComponent);
            }
        }

        private void ComponentRemoval(PhysicsComponent component)
        {
            if (component is PhysicsColliderComponent colliderComponent)
            {
                colliders.Remove(colliderComponent);

                if (colliderShapesRendering)
                {
                    colliderComponent.RemoveDebugEntity(debugScene);
                }
            }

            component.Detach();

            elements.Remove(component);
            elementIdToElement.Remove(component.Id);

            if (component is RigidbodyComponent rigidbodyComponent)
            {
                rigidElements.Remove(rigidbodyComponent);
            }
        }

        protected override void OnEntityComponentRemoved(Entity entity, PhysicsComponent component, AssociatedData data)
        {
            currentFrameRemovals.Add(component);
        }

        protected override void OnSystemAdd()
        {
            physicsSystem = Services.GetService<Bepu2PhysicsSystem>();
            if (physicsSystem == null)
            {
                physicsSystem = new Bepu2PhysicsSystem(Services);
                Services.AddService(physicsSystem);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(physicsSystem);
            }

            ((IReferencable)physicsSystem).AddReference();

            // Check if PhysicsShapesRenderingService is created (and check if rendering is enabled with IGraphicsDeviceService)
            if (Services.GetService<Graphics.IGraphicsDeviceService>() != null
                && Services.GetService<PhysicsShapesRenderingService>() == null)
            {
                debugShapeRendering = new PhysicsShapesRenderingService(Services);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(debugShapeRendering);
            }

            Simulation = physicsSystem.Create(this);

            parentScene = Services.GetSafeServiceAs<SceneSystem>()?.SceneInstance?.RootScene;
        }

        protected override void OnSystemRemove()
        {
            if (physicsSystem != null)
            {
                physicsSystem.Release(this);
                ((IReferencable)physicsSystem).Release();
            }
        }

        internal void UpdateBodies()
        {
            Dispatcher.ForEach(rigidElements, UpdateTransformations);
        }

        private void UpdateTransformations(RigidbodyComponent body)
        {
            var entity = body.Entity;

            Quaternion bodyRot;
            Vector3 bodyPos;
            if (body.Interpolate)
            {
                bodyPos = body.GetInterpolatedPosition();
                bodyRot = body.GetInterpolatedRotation();
            }
            else
            {
                bodyRot = body.GetRotation();
                bodyPos = body.GetPosition();
            }

            Matrix.Transformation(ref entity.Transform.Scale,
                ref bodyRot,
                ref bodyPos,
                out var worldMatrix);

            worldMatrix.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);
            entity.Transform.Scale = scale;
            entity.Transform.Rotation = rotation;
            entity.Transform.Position = translation;
        }

        public void UpdateRemovals()
        {
            foreach (var currentFrameRemoval in currentFrameRemovals)
            {
                ComponentRemoval(currentFrameRemoval);
            }

            currentFrameRemovals.Clear();
        }

        public bool TryGetComponentById(Guid guid, out PhysicsComponent component)
        {
            return elementIdToElement.TryGetValue(guid, out component);
        }
    }
}
