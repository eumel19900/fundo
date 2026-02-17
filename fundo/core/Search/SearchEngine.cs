using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace fundo.core.Search
{
    internal interface SearchEngine
    {
        public IAsyncEnumerable<SearchResult> SearchAsync(DirectoryInfo startDirectory, CancellationToken cancellationToken = default);

    }
}