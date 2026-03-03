using fundo;
using fundo.core;
using fundo.core.Search;
using fundo.core.Search;
using fundo.core.Search.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fundo.core.Search
{
    internal interface SearchFilter
    {
        /// <summary>
        /// Checks whether this file is allowed by the filter.
        /// Implementations should be as fast as possible and avoid unnecessary I/O.
        /// </summary>
        /// <param name="fileInfo">File to check.</param>
        /// <returns>true if the file passes the filter; otherwise false.</returns>
        bool isAllowed(FileInfo fileInfo);
    }
}
