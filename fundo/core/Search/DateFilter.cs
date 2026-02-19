using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace fundo.core.Search
{
    internal class DateFilter : SearchFilter
    {
        private DateTime startTime;
        private DateTime endTime;

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
