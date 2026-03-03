using fundo.core.Search;
using System;
using System.IO;

namespace fundo.core.Search.Index.Filter
{
    internal class IndexBasedFileNameFilter : SearchFilter
    {
        private readonly string searchPattern;

        public IndexBasedFileNameFilter(string searchPattern)
        {
            this.searchPattern = searchPattern ?? string.Empty;
        }

		public bool isAllowed(FileInfo fileInfo)
        {
            // Placeholder: implement index-based matching later. For now, allow all or apply simple pattern.
            if (fileInfo == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(searchPattern))
            {
                return true;
            }

            // Simple fallback: check if filename contains the pattern (case-insensitive)
            return fileInfo.Name.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
