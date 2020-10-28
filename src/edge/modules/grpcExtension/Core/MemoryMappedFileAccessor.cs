// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace GrpcExtension
{
    /// <summary>
    /// Allows for random access reads and writes on a memory mapped file.
    /// </summary>
    public sealed class MemoryMappedFileAccessor : IDisposable
    {
        private const string LinuxSharedMemoryDirectory = "/dev/shm";

        private readonly MemoryMappedFile _file;
        private readonly MemoryMappedViewAccessor _accessor;

        /// <summary>
        /// Gets the map name.
        /// </summary>
        public string MapName { get; }

        /// <summary>
        /// Gets the map size.
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMappedFileAccessor"/> class.
        /// </summary>
        /// <param name="mapName">Memory map name of path.</param>
        /// <param name="size">File size.</param>
        /// <param name="createIfNew">If true forces a new file to be created if one doesn't exist.</param>
        /// <param name="desiredAccess">File mode access.</param>
        public MemoryMappedFileAccessor(
            string mapName,
            long size,
            bool createIfNew = false,
            MemoryMappedFileAccess desiredAccess = MemoryMappedFileAccess.Read)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                throw new ArgumentNullException(mapName);
            }

            MapName = mapName;
            Size = size;

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
        public void Dispose()
        {
            _accessor.Dispose();
            _file.Dispose();
        }

        /// <summary>
        /// Allocates a span from the map view.
        /// </summary>
        /// <param name="offset">Offset into the view.</param>
        /// <param name="length">Span length.</param>
        /// <returns>A newly created span.</returns>
        public unsafe ReadOnlySpan<byte> Read(ulong offset, ulong length)
        {
            var viewHandle = _accessor.SafeMemoryMappedViewHandle;
            var readPointer = new IntPtr(viewHandle.DangerousGetHandle().ToInt64() + (long)offset);

            return new ReadOnlySpan<byte>(readPointer.ToPointer(), (int)length);
        }

        /// <summary>
        /// Writes a span on the map view.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="bytes"></param>
        public unsafe void Write(ulong offset, ReadOnlySpan<byte> bytes)
        {
            var length = (long)bytes.Length;

            if ((long)offset + length > Size)
            {
                throw new IndexOutOfRangeException("Offset out of the view bondary");
            }

            var viewHandle = _accessor.SafeMemoryMappedViewHandle;
            var writePointer = new IntPtr(viewHandle.DangerousGetHandle().ToInt64() + (long)offset);

            // Copy data into the shared memory
            fixed (byte* sourcePointer = bytes)
            {
                Buffer.MemoryCopy(sourcePointer, writePointer.ToPointer(), length, length);
            }
        }
    }
}
