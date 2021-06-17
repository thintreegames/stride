using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Core.Threading;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Stride.Physics
{
    public class SimpleThreadDispatcher : IThreadDispatcher, IDisposable
    {
        public int ThreadCount { get; private set; }

        private BufferPool[] buffers;

        public SimpleThreadDispatcher(int threadCount)
        {
            ThreadCount = threadCount;

            buffers = new BufferPool[ThreadCount];
            for (int i = 0; i < ThreadCount; i++)
            {
                buffers[i] = new BufferPool();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DispatchWorkers(Action<int> workerBody)
        {
            Dispatcher.For(0, ThreadCount, workerBody);
        }

        public void Dispose()
        {
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i].Clear();
                buffers[i] = null;
            }

            buffers = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferPool GetThreadMemoryPool(int workerIndex)
        {
            return buffers[workerIndex];
        }
    }
}
