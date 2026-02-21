using System;
using System.Collections.Generic;
using System.Text;

namespace fundo.core.Search.Index.Entity
{
    internal class FileEntity
    {
        // Primary key - EF Core will treat this as the identity/auto-increment column
        public long Id { get; set; }

        // drive/location where the file was found (e.g. "C:\")
        public string DriveLocation { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime FileDate { get; set; }
        public string FileType { get; set; } = string.Empty;

        public FileEntity() { }

        public FileEntity(string fileName, string path, long fileSize, DateTime fileDate,
            string fileType, string driveLocation)
        {
            FileName = fileName;
            Path = path;
            FileSize = fileSize;
            FileDate = fileDate;
            FileType = fileType;
            DriveLocation = driveLocation;
        }
    }
}
