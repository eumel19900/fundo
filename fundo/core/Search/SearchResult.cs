using System;

namespace fundo.core.Search
{
    internal class SearchResult
    {
        private String fileName;
        private int fileSize;
        private DateTime fileDate;

        public string FileName { get => fileName; set => fileName = value; }
        public int FileSize { get => fileSize; set => fileSize = value; }
        public DateTime FileDate { get => fileDate; set => fileDate = value; }


        public SearchResult(String fileName, int fileSize, DateTime fileDate)
        {
            this.fileName = fileName;
            this.fileSize = fileSize;
            this.fileDate = fileDate;
        }
    }
}
