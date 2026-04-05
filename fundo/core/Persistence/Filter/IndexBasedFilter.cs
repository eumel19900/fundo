using fundo.core.Persistence.Entity;
using fundo.core.Search;
using System.Linq;

namespace fundo.core.Persistence.Filter
{
    internal interface IIndexBasedFilter : ISearchFilter
    {
        public IQueryable<FileEntity> AddQuery(IQueryable<FileEntity> query);
    }
}
