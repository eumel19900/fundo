using fundo.core;
using fundo.core.Search.Filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace fundo.gui.Job.Jobs
{
    /// <summary>
    /// Job that filters an existing result set by file content.
    /// Uses parallel processing because regex-based content checks can be CPU-intensive.
    /// </summary>
    internal class FullTextSearchJob : JobBase
    {
        private readonly IReadOnlyList<DetachedFileInfo> _sourceResults;
        private readonly FileContentFilter _fileContentFilter;
        private readonly List<DetachedFileInfo> _results = new();

        public override string JobName => "Full-text Search";

        public IReadOnlyList<DetachedFileInfo> Results => _results;

        public FullTextSearchJob(IReadOnlyList<DetachedFileInfo> sourceResults, FileContentFilter fileContentFilter)
        {
            _sourceResults = sourceResults ?? throw new ArgumentNullException(nameof(sourceResults));
            _fileContentFilter = fileContentFilter ?? throw new ArgumentNullException(nameof(fileContentFilter));
        }

        protected override void Execute()
        {
            int totalFiles = _sourceResults.Count;
            ReportStatus("Full-text search", "Preparing content filter...");
            SetIndeterminate(false);
            ReportProgress(0, totalFiles);

            if (totalFiles == 0)
            {
                ReportStatus("Full-text search completed", "No files to filter");
                return;
            }

            bool[] matches = new bool[totalFiles];
            int processedFiles = 0;
            int matchedFiles = 0;

            ParallelOptions options = new()
            {
                CancellationToken = CancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            try
            {
                Parallel.For(0, totalFiles, options, index =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();

                    DetachedFileInfo detachedFileInfo = _sourceResults[index];
                    bool isMatch = false;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(detachedFileInfo.FullName))
                        {
                            isMatch = _fileContentFilter.IsAllowed(new FileInfo(detachedFileInfo.FullName));
                        }
                    }
                    catch
                    {
                        isMatch = false;
                    }

                    if (isMatch)
                    {
                        matches[index] = true;
                        Interlocked.Increment(ref matchedFiles);
                    }

                    int processed = Interlocked.Increment(ref processedFiles);
                    ReportProgress(processed, totalFiles);
                    ReportStatus(
                        processed,
                        "Full-text search",
                        $"Filtered {processed} of {totalFiles} files, found {Volatile.Read(ref matchedFiles)} matches");
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            for (int i = 0; i < totalFiles; i++)
            {
                ThrowIfCancellationRequested();

                if (matches[i])
                {
                    _results.Add(_sourceResults[i]);
                }
            }

            ReportProgress(totalFiles, totalFiles);
            ReportStatus("Full-text search completed", $"Filtered {totalFiles} files, found {_results.Count} matches");
        }
    }
}
