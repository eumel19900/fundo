using System;
using System.Collections.Generic;

namespace fundo.core.Persistence.Entity
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

        // Timestamp of the last completed indexing run (null if never indexed)
        public DateTime? IndexedAt { get; set; }

        // Navigation: all files that belong to this storage device
        public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    }
}
