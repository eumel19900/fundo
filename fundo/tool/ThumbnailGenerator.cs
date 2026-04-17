using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace fundo.tool
{
    /// <summary>
    /// Generates thumbnail images for image files asynchronously using multiple threads.
    /// Maintains an LRU cache limited to <see cref="MaxCacheSize"/> entries.
    /// For non-image files the file-extension icon is used instead.
    /// </summary>
    internal sealed class ThumbnailGenerator : IDisposable
    {
        public const int MaxCacheSize = 10_000;
        public const int ThumbnailSize = 120;

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".ico", ".tif", ".tiff"
        };

        // LRU cache: key = full file path
        private readonly object _cacheLock = new();
        private readonly Dictionary<string, ImageSource> _cache = new();
        private readonly LinkedList<string> _lruOrder = new();
        private readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new();

        // Pending requests: path -> list of callbacks
        private readonly ConcurrentDictionary<string, List<Action<ImageSource>>> _pending = new();

        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cts = new();

        public ThumbnailGenerator()
        {
            int threadCount = Math.Max(1, Environment.ProcessorCount);
            _semaphore = new SemaphoreSlim(threadCount, threadCount);
        }

        public static bool IsImageFile(string extension)
        {
            return ImageExtensions.Contains(extension);
        }

        /// <summary>
        /// Tries to get a cached thumbnail synchronously. Returns null if not cached.
        /// </summary>
        public ImageSource? TryGetCached(string fullPath)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(fullPath, out var img))
                {
                    TouchLru(fullPath);
                    return img;
                }
            }
            return null;
        }

        /// <summary>
        /// Requests a thumbnail for the given image file. The callback is invoked on the
        /// dispatcher thread when the thumbnail is ready. If already cached, the callback
        /// is invoked synchronously.
        /// </summary>
        public void RequestThumbnail(string fullPath, DispatcherQueue dispatcher, Action<ImageSource> callback)
        {
            // Check cache first
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(fullPath, out var cached))
                {
                    TouchLru(fullPath);
                    callback(cached);
                    return;
                }
            }

            // Coalesce duplicate requests
            var newList = new List<Action<ImageSource>> { callback };
            if (!_pending.TryAdd(fullPath, newList))
            {
                if (_pending.TryGetValue(fullPath, out var existing))
                {
                    lock (existing)
                    {
                        existing.Add(callback);
                    }
                }
                return;
            }

            // Queue background work
            _ = GenerateAsync(fullPath, dispatcher);
        }

        private async Task GenerateAsync(string fullPath, DispatcherQueue dispatcher)
        {
            try
            {
                await _semaphore.WaitAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _pending.TryRemove(fullPath, out _);
                return;
            }

            try
            {
                byte[]? pngBytes = await Task.Run(() => LoadThumbnailBytes(fullPath, _cts.Token), _cts.Token);

                if (pngBytes == null || pngBytes.Length == 0)
                {
                    _pending.TryRemove(fullPath, out _);
                    return;
                }

                dispatcher.TryEnqueue(async () =>
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.DecodePixelWidth = ThumbnailSize;
                        using var ms = new MemoryStream(pngBytes);
                        var ras = ms.AsRandomAccessStream();
                        await bmp.SetSourceAsync(ras);

                        AddToCache(fullPath, bmp);

                        if (_pending.TryRemove(fullPath, out var callbacks))
                        {
                            lock (callbacks)
                            {
                                foreach (var cb in callbacks)
                                    cb(bmp);
                            }
                        }
                    }
                    catch
                    {
                        _pending.TryRemove(fullPath, out _);
                    }
                });
            }
            catch
            {
                _pending.TryRemove(fullPath, out _);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static byte[]? LoadThumbnailBytes(string fullPath, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                using var stream = File.OpenRead(fullPath);
                using var original = System.Drawing.Image.FromStream(stream, false, false);

                int w, h;
                if (original.Width >= original.Height)
                {
                    w = ThumbnailSize;
                    h = Math.Max(1, (int)((double)original.Height / original.Width * ThumbnailSize));
                }
                else
                {
                    h = ThumbnailSize;
                    w = Math.Max(1, (int)((double)original.Width / original.Height * ThumbnailSize));
                }

                using var thumb = original.GetThumbnailImage(w, h, () => false, IntPtr.Zero);
                using var ms = new MemoryStream();
                thumb.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private void AddToCache(string key, ImageSource image)
        {
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(key))
                {
                    _cache[key] = image;
                    TouchLru(key);
                    return;
                }

                while (_cache.Count >= MaxCacheSize && _lruOrder.Count > 0)
                {
                    var oldest = _lruOrder.Last!.Value;
                    _lruOrder.RemoveLast();
                    _lruNodes.Remove(oldest);
                    _cache.Remove(oldest);
                }

                _cache[key] = image;
                var node = _lruOrder.AddFirst(key);
                _lruNodes[key] = node;
            }
        }

        private void TouchLru(string key)
        {
            if (_lruNodes.TryGetValue(key, out var node))
            {
                _lruOrder.Remove(node);
                _lruOrder.AddFirst(node);
            }
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _lruOrder.Clear();
                _lruNodes.Clear();
            }
            _pending.Clear();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _semaphore.Dispose();
            ClearCache();
        }
    }
}
