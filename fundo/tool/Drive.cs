using System;
using System.Collections.Generic;
using System.Text;

namespace fundo.tool
{
    public class Drive
    {
        public string DriveLetter { get; }
        public string NtPath { get; }
        public string VolumeGuid { get; }

        // Indicates whether this drive entry is selected in the UI.
        public bool IsSelected { get; set; }

        // Timestamp of the last completed indexing run (from StorageDevice)
        public DateTime? IndexedAt { get; set; }

        // Number of indexed files for this drive (from StorageDevice)
        public long IndexedFileCount { get; set; }

        // StorageDevice database ID (0 if not yet persisted)
        public long StorageDeviceId { get; set; }

        public string IndexedAtDisplay => IndexedAt.HasValue
            ? "Indexed at: " + IndexedAt.Value.ToString("g")
            : "Not indexed";

        public string IndexedFileCountDisplay => IndexedFileCount > 0
            ? $"{IndexedFileCount} files"
            : "No files";

        public Drive(string driveLetter, string ntPath, string volumeGuid)
        {
            DriveLetter = driveLetter;
            NtPath = ntPath;
            VolumeGuid = volumeGuid;
        }
    }
}
