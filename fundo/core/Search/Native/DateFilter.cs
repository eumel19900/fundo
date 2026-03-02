using fundo.core.Search.Filter;
using System;
using System.IO;

namespace fundo.core.Search.Native
{
    internal class DateFilter : BaseDateFilter
    {
        public DateFilter(DateTime startTime, DateTime endTime)
            : base(startTime, endTime)
        {
        }

        public override bool isAllowed(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return false;
            }

            return fileInfo.CreationTime >= startTime && fileInfo.CreationTime <= endTime;
        }
    }
}
