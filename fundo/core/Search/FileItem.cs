using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fundo.core.Search
{
    internal class FileItem
    {
        private string fileName;
        private string fullName;
        private long fileSize;
        private DateTime dateCreated;
        private DateTime dateModified;
        private DateTime dateLastAccess;

        // List of selected file attributes (e.g. ReadOnly, Hidden, System, ...)
        public List<FileAttributes> Attributes { get; } = new List<FileAttributes>();

        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        public string FullName
        {
            get => fullName;
            set => fullName = value;
        }

        public long FileSize
        {
            get => fileSize;
            set => fileSize = value;
        }

        public DateTime FileDate
        {
            get => dateCreated;
            set => dateCreated = value;
        }

        public FileItem()
        {
        }

        public FileItem(string fileName, string fullName, long fileSize, DateTime fileDate)
            : this(fileName, fullName, fileSize, fileDate, fileDate, fileDate)
        {
        }

        public FileItem(string fileName, string fullName, long fileSize,
                        DateTime dateCreated, DateTime dateModified, DateTime dateLastAccess)
        {
            this.fileName = fileName;
            this.fullName = fullName;
            this.fileSize = fileSize;
            this.dateCreated = dateCreated;
            this.dateModified = dateModified;
            this.dateLastAccess = dateLastAccess;
        }

        public FileItem(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

            fileName = fileInfo.Name;
            fullName = fileInfo.FullName;
            fileSize = fileInfo.Length;
            dateCreated = fileInfo.CreationTime;
            dateModified = fileInfo.LastWriteTime;
            dateLastAccess = fileInfo.LastAccessTime;

            // Map FileInfo.Attributes flags to the attribute list used in the UI.
            var attrs = fileInfo.Attributes;

            if ((attrs & FileAttributes.ReadOnly) != 0)
            {
                Attributes.Add(FileAttributes.ReadOnly);
            }
            if ((attrs & FileAttributes.Hidden) != 0)
            {
                Attributes.Add(FileAttributes.Hidden);
            }
            if ((attrs & FileAttributes.System) != 0)
            {
                Attributes.Add(FileAttributes.System);
            }
            if ((attrs & FileAttributes.Archive) != 0)
            {
                Attributes.Add(FileAttributes.Archive);
            }
            if ((attrs & FileAttributes.Temporary) != 0)
            {
                Attributes.Add(FileAttributes.Temporary);
            }
            if ((attrs & FileAttributes.Compressed) != 0)
            {
                Attributes.Add(FileAttributes.Compressed);
            }
            if ((attrs & FileAttributes.Encrypted) != 0)
            {
                Attributes.Add(FileAttributes.Encrypted);
            }
        }
    }
}
