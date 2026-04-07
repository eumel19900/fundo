using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fundo.core.Search.Filter
{
    internal class FileSizeFilter : INativeSearchFilter
    {
        private long fileSize;
        private FileSizeCompareMode compareMode;

        public FileSizeFilter(long fileSize, FileSizeCompareMode compareMode)
        {
            this.fileSize = fileSize;
            this.compareMode = compareMode;
        }

        public bool IsAllowed(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return false;
            }

            switch(compareMode)
            {
                case FileSizeCompareMode.Equals:
                    return fileInfo.Length == fileSize;
                case FileSizeCompareMode.BiggerThan:
                    return fileInfo.Length > fileSize;
                case FileSizeCompareMode.SmallerThan:
                    return fileInfo.Length < fileSize;
            }

            return false;
        }
    }
}
