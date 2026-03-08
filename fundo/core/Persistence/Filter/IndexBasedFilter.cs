using fundo.core.Persistence.Entity;
using fundo.core.Search;
using System.Linq;

namespace fundo.core.Persistence.Filter
{
    internal interface IndexBasedFilter : SearchFilter
    {
        public IQueryable<FileEntity> addQuery(IQueryable<FileEntity> query);
    }
}
