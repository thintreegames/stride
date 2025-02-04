// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;

namespace Stride.Physics
{
    public interface IPhysicsSystem : IGameSystemBase
    {
        Simulation Create(PhysicsProcessor sceneProcessor, PhysicsEngineFlags flags = PhysicsEngineFlags.None);
        void Release(PhysicsProcessor processor);
    }
}
