using fundo.core.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace fundo.core.Search.Native
{
    internal interface SearchEngine
    {
        public IAsyncEnumerable<SearchResultItem> SearchAsync(DirectoryInfo startDirectory, 
            CancellationToken cancellationToken, 
            List<SearchFilter> searchFilters);

        /// <summary>
        /// This resets the engine and the search statics. Call this before calling SearchAsync.
        /// </summary>
        public void reset();
    }
}