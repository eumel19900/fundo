using System;
using System.IO;

namespace fundo.core.Search
{
    internal class SearchResult
    {
        private String fileName;
        private long fileSize;
        private DateTime fileDate;
        private FileInfo fileInfo;

        public string FileName { get => fileName; set => fileName = value; }
        public long FileSize { get => fileSize; set => fileSize = value; }
        public DateTime FileDate { get => fileDate; set => fileDate = value; }
        public FileInfo FileInfo { get => fileInfo; set => fileInfo = value; }


        public SearchResult(FileInfo fileInfo)
        {
            this.fileName = fileInfo.FullName;
            this.fileSize = fileInfo.Length;
            this.fileDate = fileInfo.CreationTime;
            this.fileInfo = fileInfo;
        }
    }
}
