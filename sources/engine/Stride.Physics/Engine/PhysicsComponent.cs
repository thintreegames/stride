// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine.Design;
using Stride.Physics;
using Stride.Physics.Engine;
using Stride.Physics.Events;
using Stride.Rendering;

namespace Stride.Engine
{
    [DataContract("PhysicsComponent", Inherited = true)]
    [Display("Physics", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(PhysicsProcessor))]
    [AllowMultipleComponents]
    [ComponentOrder(3000)]
    [ComponentCategory("Physics")]
    public abstract class PhysicsComponent : ActivableEntityComponent
    {
        public delegate void PhysicsEventCallback<T>(PhysicsComponent actingComponent, T eventData) where T : IPhysicsEventData;

        protected static Logger logger = GlobalLogger.GetLogger("PhysicsComponent");

        [DataMemberIgnore]
        public Simulation Simulation { get; internal set; }

        [DataMemberIgnore]
        protected PhysicsProcessor.AssociatedData Data { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>
        /// The tag.
        /// </value>
        [DataMemberIgnore]
        public string Tag { get; set; }

        [DataMemberIgnore]
        internal PhysicsShapesRenderingService DebugShapeRendering;

        [DataMemberIgnore]
        internal IDictionary<Type, HashSet<Delegate>> physicsEventCallbacks;

        [DataMemberIgnore]
        public Entity DebugEntity { get; set; }

        public PhysicsComponent()
        {
            physicsEventCallbacks = new Dictionary<Type, HashSet<Delegate>>();
        }

        internal void Attach(PhysicsProcessor.AssociatedData data)
        {
            Data = data;

            if (Simulation.DisableSimulation)
            {
                return;
            }

            OnAttach();
            Enabled = base.Enabled;
        }

        internal void Detach()
        {
            Data = null;

            if (Simulation.DisableSimulation)
            {
                return;
            }

            OnDetach();
        }

        protected virtual void OnAttach()
        {

        }

        protected virtual void OnDetach()
        {

        }

        public void SubscribeToEvent<T>(PhysicsEventCallback<T> callback) where T : IPhysicsEventData
        {
            Type t = typeof(T);
            if (!physicsEventCallbacks.TryGetValue(t, out var set))
            {
                set = new HashSet<Delegate>();
                physicsEventCallbacks.Add(t, set);
            }
            set.Add(callback);
        }

        public void UnsubscribeFromEvent<T>(PhysicsEventCallback<T> callback) where T : IPhysicsEventData
        {
            Type t = typeof(T);
            if (!physicsEventCallbacks.TryGetValue(t, out var set)) return;
            set.Remove(callback);
        }

    }
}
