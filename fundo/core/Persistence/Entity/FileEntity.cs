using System;
using System.ComponentModel.DataAnnotations.Schema;
using fundo.core;

namespace fundo.core.Persistence.Entity
{
    internal class FileEntity
    {
        // Primary key - EF Core will treat this as the identity/auto-increment column
        public long Id { get; set; }

        // Foreign key to the storage device where the file resides
        public long StorageDeviceId { get; set; }

        // Navigation to the owning storage device
        public StorageDevice StorageDevice { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public string FileType { get; set; } = string.Empty;
        public byte FileAttributesValue { get; set; }

        [NotMapped]
        public FileAttribute FileAttributes
        {
            get => FileAttributeHelper.FromByte(FileAttributesValue);
            set => FileAttributesValue = FileAttributeHelper.ToByte(value);
        }

        public FileEntity() { }

        public FileEntity(string fileName, string path, long fileSize, DateTime creationTime,
            DateTime modifiedTime, DateTime lastAccessTime, string fileType, FileAttribute fileAttributes = FileAttribute.None)
        {
            FileName = fileName;
            Path = path;
            FileSize = fileSize;
            CreationTime = creationTime;
            ModifiedTime = modifiedTime;
            LastAccessTime = lastAccessTime;
            FileType = fileType;
            FileAttributes = fileAttributes;
        }
    }
}
