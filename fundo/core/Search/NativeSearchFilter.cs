using System.IO;

namespace fundo.core.Search
{
    internal interface INativeSearchFilter : ISearchFilter
    {
        /// <summary>
        /// Checks whether this file is allowed by the filter.
        /// Implementations should be as fast as possible and avoid unnecessary I/O.
        /// </summary>
        /// <param name="fileInfo">File to check.</param>
        /// <returns>true if the file passes the filter; otherwise false.</returns>
        bool IsAllowed(FileInfo fileInfo);
    }
}
