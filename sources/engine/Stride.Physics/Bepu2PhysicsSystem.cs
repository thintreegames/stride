// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.Physics
{
    public class Bepu2PhysicsSystem : GameSystem, IPhysicsSystem
    {
        private class PhysicsScene
        {
            /// <summary>
            /// Physics Scene
            /// </summary>
            public PhysicsProcessor Processor { get; private set; }

            /// <summary>
            /// The Simulation
            /// </summary>
            public Simulation Simulation { get; private set; }

            public PhysicsScene(PhysicsProcessor processor, Simulation simulation)
            {
                Processor = processor;
                Simulation = simulation;
            }
        }


        //Note that the buffer pool used by the simulation is not considered to be *owned* by the simulation. The simulation merely uses the pool.
        //Disposing the simulation will not dispose or clear the buffer pool.
        /// <summary>
        /// Gets the buffer pool used by the demo's simulation.
        /// </summary>
        public BufferPool BufferPool { get; private set; }

        /// <summary>
        /// Gets the thread dispatcher available for use by the simulation.
        /// </summary>
        public SimpleThreadDispatcher ThreadDispatcher { get; private set; }

        private readonly List<PhysicsScene> scenes = new List<PhysicsScene>();

        public Bepu2PhysicsSystem(IServiceRegistry registry)
            : base(registry)
        {
            UpdateOrder = -1000;
            Enabled = true;
        }


        public override void Initialize()
        {
            base.Initialize();

            BufferPool = new BufferPool();
            //Generally, shoving as many threads as possible into the simulation won't produce the best results on systems with multiple logical cores per physical core.
            //Environment.ProcessorCount reports logical core count only, so we'll use a simple heuristic here- it'll leave one or two logical cores idle.
            //For the common Intel quad core with hyperthreading, this'll use six logical cores and leave two logical cores free to be used for other stuff.
            //This is by no means perfect. To maximize performance, you'll need to profile your simulation and target hardware.
            //Note that issues can be magnified on older operating systems like Windows 7 if all logical cores are given work.

            //Generally, the more memory bandwidth you have relative to CPU compute throughput, and the more collision detection heavy the simulation is relative to solving,
            //the more benefit you get out of SMT/hyperthreading. 
            //For example, if you're using the 64 core quad memory channel AMD 3990x on a scene composed of thousands of ragdolls, 
            //there won't be enough memory bandwidth to even feed half the physical cores. Using all 128 logical cores would just add overhead.
            ThreadDispatcher = new SimpleThreadDispatcher(Dispatcher.MaxDegreeOfParallelism);
        }

        public Simulation Create(PhysicsProcessor sceneProcessor, PhysicsEngineFlags flags = PhysicsEngineFlags.None)
        {
            var scene = new PhysicsScene(sceneProcessor, new Simulation(sceneProcessor, BufferPool, ThreadDispatcher));

            lock (this)
            {
                scenes.Add(scene);
            }

            return scene.Simulation;
        }

        public void Release(PhysicsProcessor processor)
        {
            lock (this)
            {
                var scene = scenes.SingleOrDefault(x => x.Processor == processor);
                if (scene == null) return;
                scenes.Remove(scene);

                scene.Simulation.Dispose();
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            lock (this)
            {
                foreach (var scene in scenes)
                {
                    scene.Simulation.Dispose();
                }
            }

            BufferPool.Clear();
            ThreadDispatcher.Dispose();
        }

        public override void Update(GameTime gameTime)
        {
            if (Simulation.DisableSimulation) return;

            if (gameTime.TimePerFrame.TotalSeconds <= 0) return;

            lock (this)
            {
                foreach (var physicsScene in scenes)
                {
                    //first process any needed cleanup
                    physicsScene.Processor.UpdateRemovals();

                    physicsScene.Simulation.Simulate(gameTime.TimePerFrame.TotalSeconds, ThreadDispatcher);

                    // physicsScene.Processor.UpdateCharacters();

                    physicsScene.Processor.UpdateBodies();

                    physicsScene.Simulation.SendEvents();
                }
            }
        }


    }
}
