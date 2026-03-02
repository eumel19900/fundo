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
        }
    }
}
