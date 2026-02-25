using System.Collections.Generic;

namespace fundo.core.Search.Index.Entity
{
    /// <summary>
    /// Represents a physical or logical storage device (e.g. NT device name or network drive).
    /// </summary>
    internal class StorageDevice
    {
        // Primary key - identity/auto-increment column
        public long Id { get; set; }

        // Device name as used by Windows (e.g. NT device path or network device name)
        public string StorageName { get; set; } = string.Empty;

        // Navigation: all files that belong to this storage device
        public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    }
}
