using GrpcExtension.Processors;
using System;
using System.Threading;

namespace GrpcExtension.Core
{
    /// <summary>
    /// Keeps stream processing state that needs to be disposed on stream completion.
    /// </summary>
    public class StreamState : IDisposable
    {
        public IBatchProcessor Processor { get; set; }
        public MemoryMappedFileMemoryManager<byte> MemoryMappedFile { get; set; }
        public MediaDescriptor ClientDescriptor { get; set; }

        public void Dispose()
        {
            ((IDisposable)MemoryMappedFile)?.Dispose();
        }
    }
}