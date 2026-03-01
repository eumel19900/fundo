using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace fundo.core.Search.Native
{
    internal class NativeSearchEngine : SearchEngine
    {
        private int directoriesSearched = 0;

        public int DirectoriesSearched { get => directoriesSearched; }

        public Boolean LoadFileIcons { get; set; } = true;

        public void reset()
        {
            directoriesSearched = 0;
        }

        public async IAsyncEnumerable<SearchResultItem> SearchAsync(DirectoryInfo startDirectory,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            List<SearchFilter> searchFilters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (startDirectory == null || !startDirectory.Exists)
            {
                yield break;
            }

            // Use a bounded channel to stream results from a background producer to the async iterator consumer.
            var channel = Channel.CreateBounded<SearchResultItem>(new BoundedChannelOptions(256)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            var writer = channel.Writer;

            // Producer: run directory traversal on threadpool and write matching items into channel.
            var produceTask = Task.Run(async () =>
            {
                try
                {
                    var directoriesStack = new Stack<DirectoryInfo>();
                    directoriesStack.Push(startDirectory);

                    while (directoriesStack.Count > 0)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var dir = directoriesStack.Pop();
                        directoriesSearched++;

                        IEnumerable<FileInfo> files = null;
                        try
                        {
                            files = dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                        }
                        catch (UnauthorizedAccessException) { continue; }
                        catch (DirectoryNotFoundException) { continue; }
                        catch (IOException) { continue; }

                        foreach (var file in files)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            bool allowed = true;

                            // Nur filtern wenn searchFilters vorhanden sind
                            if (searchFilters != null && searchFilters.Count > 0)
                            {
                                try
                                {
                                    foreach (var filter in searchFilters)
                                    {
                                        if (!filter.isAllowed(file))
                                        {
                                            allowed = false;
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    // if a filter throws, treat as not allowed
                                    allowed = false;
                                }
                            }

                            if (!allowed) continue;

                            try
                            {
                                var item = new SearchResultItem(file,LoadFileIcons);
                                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { break; }
                            catch
                            {
                                // ignore file-specific failures
                                continue;
                            }
                        }

                        // push subdirectories
                        IEnumerable<DirectoryInfo> subdirs = null;
                        try
                        {
                            subdirs = dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
                        }
                        catch (UnauthorizedAccessException) { continue; }
                        catch (DirectoryNotFoundException) { continue; }
                        catch (IOException) { continue; }

                        foreach (var sd in subdirs)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            directoriesStack.Push(sd);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    writer.TryComplete(ex);
                    return;
                }

                writer.TryComplete();
            }, cancellationToken);

            var reader = channel.Reader;

            try
            {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var item))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return item;
                    }
                }
            }
            finally
            {
                // ensure producer task finished
                try { await produceTask.ConfigureAwait(false); } catch { }
            }
        }
    }
}
