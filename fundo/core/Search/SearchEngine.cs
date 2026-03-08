using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace fundo.core.Search
{
    internal interface SearchEngine
    {
        [Flags]
        public enum EngineType
        {
            Native = 1,
            IndexBased = 2
        }

        /// <summary>
        /// Identifies the concrete engine implementation (native filesystem vs. index based).
        /// </summary>
        EngineType Kind { get; }

        public IAsyncEnumerable<SearchResultItem> SearchAsync(DirectoryInfo startDirectory, 
            CancellationToken cancellationToken, 
            List<SearchFilter> searchFilters);

        /// <summary>
        /// This resets the engine and the search statics. Call this before calling SearchAsync.
        /// </summary>
        public void reset();
    }
}