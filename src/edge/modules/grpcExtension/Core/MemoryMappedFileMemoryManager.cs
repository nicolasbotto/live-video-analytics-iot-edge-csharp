// -----------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Buffers;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace grpcExtension.Core
{
    /// <summary>
    /// Allows for random access reads and writes on a memory mapped file.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    public sealed class MemoryMappedFileMemoryManager<T> : MemoryManager<T>
        where T : unmanaged
    {
        private const string LinuxSharedMemoryDirectory = "/dev/shm";

        private readonly int _length;
        private readonly MemoryMappedFile _file;
        private readonly MemoryMappedViewAccessor _accessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMappedFileAccessor"/> class.
        /// </summary>
        /// <param name="mapName">Memory map name of path.</param>
        /// <param name="length">Length of the map determined in number of items.</param>
        /// <param name="createIfNew">If true forces a new file to be created if one doesn't exist.</param>
        /// <param name="desiredAccess">File mode access.</param>
        public unsafe MemoryMappedFileMemoryManager(
            string mapName,
            int length,
            bool createIfNew = false,
            MemoryMappedFileAccess desiredAccess = MemoryMappedFileAccess.Read)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            _length = length;

            var size = _length * sizeof(T);

            // Create the memory-mapped file.
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                mapName = Path.Combine(LinuxSharedMemoryDirectory, mapName);
                _file = MemoryMappedFile.CreateFromFile(mapName, createIfNew ? FileMode.OpenOrCreate : FileMode.Open, null, size);
            }
            else
            {
                _file = createIfNew
                    ? MemoryMappedFile.CreateOrOpen(mapName, size, desiredAccess)
                    : MemoryMappedFile.OpenExisting(mapName, desiredAccess.ToMemoryMappedFileRights());
            }

            _accessor = _file.CreateViewAccessor(0, size, desiredAccess);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _accessor.Dispose();
                _file.Dispose();
            }
        }

        /// <inheritdoc />
        public unsafe override Span<T> GetSpan()
        {
            var viewHandle = _accessor.SafeMemoryMappedViewHandle;
            var viewPointer = viewHandle.DangerousGetHandle();

            return new Span<T>((void*)viewPointer, _length);
        }

        /// <inheritdoc />
        public unsafe override MemoryHandle Pin(int elementIndex = 0)
        {
            var viewHandle = _accessor.SafeMemoryMappedViewHandle;
            var viewPointer = viewHandle.DangerousGetHandle() + elementIndex * sizeof(T);

            return new MemoryHandle((void*)viewPointer);
        }

        /// <inheritdoc />
        public override void Unpin()
        {
        }
    }
}
