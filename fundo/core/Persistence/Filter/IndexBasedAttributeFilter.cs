using fundo.core.Persistence.Entity;
using System.Linq;

namespace fundo.core.Persistence.Filter
{
    internal class IndexBasedAttributeFilter : IIndexBasedFilter
    {
        private readonly byte requiredAttributesValue;

        public IndexBasedAttributeFilter(FileAttribute requiredAttributes)
        {
            requiredAttributesValue = FileAttributeHelper.ToByte(requiredAttributes);
        }

        public IQueryable<FileEntity> AddQuery(IQueryable<FileEntity> query)
        {
            if (requiredAttributesValue == 0)
            {
                return query.Where(f => false);
            }

            return query.Where(f => (f.FileAttributesValue & requiredAttributesValue) == requiredAttributesValue);
        }
    }
}
