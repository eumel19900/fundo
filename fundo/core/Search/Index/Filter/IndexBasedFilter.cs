using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fundo.core.Search.Index.Filter
{
    internal interface IndexBasedFilter : SearchFilter
    {
        public IQueryable<Entity.FileEntity> addQuery(IQueryable<Entity.FileEntity> query);
    }
}
