using fundo.core.Search;
using System;
using System.IO;


namespace fundo.core.Search.Native.Filter
{
	internal class DateFilter : SearchFilter
    {
        private readonly DateTime startTime;
        private readonly DateTime endTime;

        public DateFilter(DateTime startTime, DateTime endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public bool isAllowed(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return false;
            }

            return fileInfo.CreationTime >= startTime && fileInfo.CreationTime <= endTime;
        }
    }
}
