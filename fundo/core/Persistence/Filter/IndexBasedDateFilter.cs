using fundo.core.Persistence.Entity;
using System;
using System.Linq;

namespace fundo.core.Persistence.Filter
{
    internal class IndexBasedDateFilter : IndexBasedFilter
    {
        private readonly DateTime startTime;
        private readonly DateTime endTime;
        private readonly bool useCreationTime;
        private readonly bool useModifiedTime;
        private readonly bool useLastAccessTime;

        public IndexBasedDateFilter(
            DateTime startTime,
            DateTime endTime,
            bool useCreationTime,
            bool useModifiedTime,
            bool useLastAccessTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.useCreationTime = useCreationTime;
            this.useModifiedTime = useModifiedTime;
            this.useLastAccessTime = useLastAccessTime;
        }

        public IQueryable<FileEntity> addQuery(IQueryable<FileEntity> query)
        {
            if (!useCreationTime && !useModifiedTime && !useLastAccessTime)
            {
                return query.Where(f => false);
            }

            return query.Where(f =>
                (useCreationTime && f.CreationTime >= startTime && f.CreationTime <= endTime) ||
                (useModifiedTime && f.ModifiedTime >= startTime && f.ModifiedTime <= endTime) ||
                (useLastAccessTime && f.LastAccessTime >= startTime && f.LastAccessTime <= endTime));
        }
    }
}
