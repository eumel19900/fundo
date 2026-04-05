using fundo.core.Persistence.Entity;
using fundo.core.Search;
using System.Linq;

namespace fundo.core.Persistence.Filter
{
    internal class IndexBasedFileSizeFilter : IIndexBasedFilter
    {
        private readonly long fileSize;
        private readonly FileSizeCompareMode compareMode;

        public IndexBasedFileSizeFilter(long fileSize, FileSizeCompareMode compareMode)
        {
            this.fileSize = fileSize * 1024;
            this.compareMode = compareMode;
        }

        public IQueryable<FileEntity> AddQuery(IQueryable<FileEntity> query)
        {
            return compareMode switch
            {
                FileSizeCompareMode.Equals => query.Where(f => f.FileSize == fileSize),
                FileSizeCompareMode.BiggerThan => query.Where(f => f.FileSize > fileSize),
                FileSizeCompareMode.SmallerThan => query.Where(f => f.FileSize < fileSize),
                _ => query.Where(f => f.FileSize == fileSize)
            };
        }
    }
}
