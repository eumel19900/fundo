using System.IO;

namespace fundo.core.Search.Filter
{
    internal class AttributeFilter : INativeSearchFilter
    {
        private readonly FileAttribute requiredAttributes;

        public AttributeFilter(FileAttribute requiredAttributes)
        {
            this.requiredAttributes = requiredAttributes;
        }

        public bool IsAllowed(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return false;
            }

            if (requiredAttributes == FileAttribute.None)
            {
                return false;
            }

            FileAttribute fileAttributes = FileAttributeHelper.FromSystemFileAttributes(fileInfo.Attributes);
            return FileAttributeHelper.HasAttribute(fileAttributes, requiredAttributes);
        }
    }
}
