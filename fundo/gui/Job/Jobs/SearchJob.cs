using fundo.core.Search;
using fundo.core.Search.Filter;
using System;
using System.Collections.Generic;
using System.IO;

namespace fundo.gui.Job.Jobs
{
    /// <summary>
    /// Job that performs a file search using the configured search engine.
    /// Collects results into a list that can be retrieved after completion.
    /// </summary>
    internal class SearchJob : JobBase
    {
        private readonly SearchEngine _searchEngine;
        private readonly List<DirectoryInfo> _rootSearchDirectories;
        private readonly List<SearchFilter> _filters;
        private readonly List<SearchResultItem> _results = new();

        public override string JobName => "File Search";

        /// <summary>
        /// The search results collected during execution.
        /// </summary>
        public IReadOnlyList<SearchResultItem> Results => _results;

        public SearchJob(SearchEngine searchEngine, List<DirectoryInfo> rootSearchDirectories, List<SearchFilter> filters)
        {
            _searchEngine = searchEngine ?? throw new ArgumentNullException(nameof(searchEngine));
            _rootSearchDirectories = rootSearchDirectories ?? throw new ArgumentNullException(nameof(rootSearchDirectories));
            _filters = filters ?? throw new ArgumentNullException(nameof(filters));
        }

        protected override void Execute()
        {
            _searchEngine.reset();
            //SetIndeterminate(true);
            ReportStatus("Searching", "Preparing search...");

            int directoryIndex = 0;
            foreach (DirectoryInfo rootDir in _rootSearchDirectories)
            {
                ThrowIfCancellationRequested();

                directoryIndex++;
                if (_rootSearchDirectories.Count > 1)
                {
                    ReportTitle($"Searching in directory {directoryIndex} of {_rootSearchDirectories.Count}...");
                }

                var asyncEnumerable = _searchEngine.SearchAsync(rootDir, CancellationToken, _filters);
                var enumerator = asyncEnumerable.GetAsyncEnumerator(CancellationToken);
                try
                {
                    while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                    {
                        _results.Add(enumerator.Current);

                        if (_searchEngine is NativeSearchEngine nativeEngine)
                        {
                            ReportDescription($"Searched {nativeEngine.DirectoriesSearched} directories, found {_results.Count} items");
                        }
                        else
                        {
                            ReportDescription($"Found {_results.Count} items");
                        }
                    }
                }
                finally
                {
                    enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }

            //SetIndeterminate(false);
            ReportStatus("Search completed", $"Found {_results.Count} items");
        }
    }
}
