using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fundo.core.Search
{
    internal class NativeSearchEngine : SearchEngine
    {
        public async IAsyncEnumerable<SearchResult> SearchAsync(DirectoryInfo startDirectory,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            List<SearchFilter> searchFilters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Enumerate files and create SearchResult objects on a background thread
            List<SearchResult> fileResults;
            try
            {
                fileResults = await Task.Run(() =>
                {
                    var list = new List<SearchResult>();

                    IEnumerable<FileInfo> files;
                    try
                    {
                        files = startDirectory.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return list;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return list;
                    }
                    catch (IOException)
                    {
                        return list;
                    }

                    foreach (FileInfo file in files)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        try
                        {
                            list.Add(new SearchResult(file));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            continue;
                        }
                        catch (IOException)
                        {
                            continue;
                        }
                    }

                    return list;
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            foreach (SearchResult result in fileResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool allowed = true;
                foreach (SearchFilter filter in searchFilters)
                {
                    if (!filter.isAllowed(result.FileInfo))
                    {
                        allowed = false;
                        break;
                    }
                }

                if (allowed)
                {
                    yield return result;
                }
            }

            // Enumerate directories on a background thread to avoid blocking the UI
            List<DirectoryInfo> directories;
            try
            {
                directories = await Task.Run(() =>
                {
                    try
                    {
                        return new List<DirectoryInfo>(startDirectory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return new List<DirectoryInfo>();
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return new List<DirectoryInfo>();
                    }
                    catch (IOException)
                    {
                        return new List<DirectoryInfo>();
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            foreach (var directory in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await foreach (var result in SearchAsync(directory, cancellationToken, searchFilters))
                {
                    yield return result;
                }
            }
        }
    }
}
