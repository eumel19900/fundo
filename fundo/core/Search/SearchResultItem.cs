using fundo.gui.tool;
using System;
using System.IO;

namespace fundo.core.Search
{
    internal class SearchResultItem
    {
        private String fileName;
        private String path;
        private long fileSize;
        private DateTime fileDate;
        private FileInfo fileInfo;

        public string FileName { get => fileName; set => fileName = value; }
        public long FileSize { get => fileSize; set => fileSize = value; }
        public DateTime FileDate { get => fileDate; set => fileDate = value; }
        public FileInfo FileInfo { get => fileInfo; set => fileInfo = value; }
        public string Path { get => path; set => path = value; }
        public string FileSizeString => FileSizeStringHelper.ToHumanReadable(fileSize);

        public SearchResultItem()
        {

        }


        public SearchResultItem(FileInfo fileInfo)
        {
            this.fileName = fileInfo.Name;
            this.path = fileInfo.FullName;
            this.fileSize = fileInfo.Length;
            this.fileDate = fileInfo.CreationTime;
            this.fileInfo = fileInfo;
        }
    }
}
