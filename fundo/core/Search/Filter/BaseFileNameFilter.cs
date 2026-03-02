using System.IO;

namespace fundo.core.Search.Filter
{
    internal abstract class BaseFileNameFilter : SearchFilter
    {
        protected readonly string searchPattern;

        protected BaseFileNameFilter(string searchPattern)
        {
            // Null abfangen, damit abgeleitete Klassen keinen Null-Check brauchen
            this.searchPattern = searchPattern ?? string.Empty;
        }

        // Muss von der abgeleiteten Klasse implementiert werden
        public abstract bool isAllowed(FileInfo fileInfo);
    }
}