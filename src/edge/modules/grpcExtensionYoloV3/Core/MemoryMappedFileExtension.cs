using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

namespace grpcExtension
{
    internal static class MemoryMappedFileExtensions
    {
        /// <summary>
        /// Converts a MemoryMappedFileAccess enum value to a equivalend MemoryMappedFileRights value.
        /// </summary>
        /// <param name="fileAccess">Value to be converted.</param>
        /// <returns>Converted value.</returns>
        public static MemoryMappedFileRights ToMemoryMappedFileRights(this MemoryMappedFileAccess fileAccess)
        {
            switch (fileAccess)
            {
                case MemoryMappedFileAccess.ReadWrite:

                    return MemoryMappedFileRights.ReadWrite;

                case MemoryMappedFileAccess.Read:

                    return MemoryMappedFileRights.Read;

                case MemoryMappedFileAccess.Write:

                    return MemoryMappedFileRights.Write;

                case MemoryMappedFileAccess.CopyOnWrite:

                    return MemoryMappedFileRights.CopyOnWrite;

                case MemoryMappedFileAccess.ReadExecute:

                    return MemoryMappedFileRights.ReadExecute;

                case MemoryMappedFileAccess.ReadWriteExecute:

                    return MemoryMappedFileRights.ReadWriteExecute;

                default:

                    throw new NotSupportedException($"File access conversion not supported: {fileAccess}");
            }
        }
    }
}
