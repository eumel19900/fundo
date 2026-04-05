using fundo.core.Search.Filter;
using System;
using System.IO;


namespace fundo.core.Search.Filter
{
	internal class DateFilter : INativeSearchFilter
	{
		private readonly DateTime startTime;
		private readonly DateTime endTime;
		private readonly bool useCreationTime;
		private readonly bool useModifiedTime;
		private readonly bool useLastAccessTime;

		public DateFilter(DateTime startTime, DateTime endTime, bool useCreationTime, bool useModifiedTime, bool useLastAccessTime)
		{
			this.startTime = startTime;
			this.endTime = endTime;
			this.useCreationTime = useCreationTime;
			this.useModifiedTime = useModifiedTime;
			this.useLastAccessTime = useLastAccessTime;
		}

		public bool IsAllowed(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return false;
            }

            if (!useCreationTime && !useModifiedTime && !useLastAccessTime)
            {
                return false;
            }

            if (useCreationTime && fileInfo.CreationTime >= startTime && fileInfo.CreationTime <= endTime)
            {
                return true;
            }

            if (useModifiedTime && fileInfo.LastWriteTime >= startTime && fileInfo.LastWriteTime <= endTime)
            {
                return true;
            }

            if (useLastAccessTime && fileInfo.LastAccessTime >= startTime && fileInfo.LastAccessTime <= endTime)
            {
                return true;
            }

            return false;
        }
    }
}
