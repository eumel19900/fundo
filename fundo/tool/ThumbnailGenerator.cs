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

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".ico", ".tif", ".tiff"
        };

        // LRU cache: key = full file path + size
        private readonly object _cacheLock = new();
        private readonly Dictionary<string, ImageSource> _cache = new();
        private readonly LinkedList<string> _lruOrder = new();
        private readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new();

        // Pending requests: full file path + size -> list of callbacks
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
        public ImageSource? TryGetCached(string fullPath, int thumbnailSize)
        {
            string cacheKey = CreateCacheKey(fullPath, thumbnailSize);

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var img))
                {
                    TouchLru(cacheKey);
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
        public void RequestThumbnail(string fullPath, int thumbnailSize, DispatcherQueue dispatcher, Action<ImageSource> callback)
        {
            string cacheKey = CreateCacheKey(fullPath, thumbnailSize);

            // Check cache first
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached))
                {
                    TouchLru(cacheKey);
                    callback(cached);
                    return;
                }
            }

            // Coalesce duplicate requests
            var newList = new List<Action<ImageSource>> { callback };
            if (!_pending.TryAdd(cacheKey, newList))
            {
                if (_pending.TryGetValue(cacheKey, out var existing))
                {
                    lock (existing)
                    {
                        existing.Add(callback);
                    }
                }
                return;
            }

            // Queue background work
            _ = GenerateAsync(fullPath, thumbnailSize, cacheKey, dispatcher);
        }

        private async Task GenerateAsync(string fullPath, int thumbnailSize, string cacheKey, DispatcherQueue dispatcher)
        {
            try
            {
                await _semaphore.WaitAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _pending.TryRemove(cacheKey, out _);
                return;
            }

            try
            {
                byte[]? pngBytes = await Task.Run(() => LoadThumbnailBytes(fullPath, thumbnailSize, _cts.Token), _cts.Token);

                if (pngBytes == null || pngBytes.Length == 0)
                {
                    _pending.TryRemove(cacheKey, out _);
                    return;
                }

                dispatcher.TryEnqueue(async () =>
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.DecodePixelWidth = thumbnailSize;
                        using var ms = new MemoryStream(pngBytes);
                        var ras = ms.AsRandomAccessStream();
                        await bmp.SetSourceAsync(ras);

                        AddToCache(cacheKey, bmp);

                        if (_pending.TryRemove(cacheKey, out var callbacks))
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
                        _pending.TryRemove(cacheKey, out _);
                    }
                });
            }
            catch
            {
                _pending.TryRemove(cacheKey, out _);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static byte[]? LoadThumbnailBytes(string fullPath, int thumbnailSize, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                using var stream = File.OpenRead(fullPath);
                using var original = System.Drawing.Image.FromStream(stream, false, false);

                int w, h;
                if (original.Width >= original.Height)
                {
                    w = thumbnailSize;
                    h = Math.Max(1, (int)((double)original.Height / original.Width * thumbnailSize));
                }
                else
                {
                    h = thumbnailSize;
                    w = Math.Max(1, (int)((double)original.Width / original.Height * thumbnailSize));
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

        private static string CreateCacheKey(string fullPath, int thumbnailSize)
        {
            return $"{fullPath}|{thumbnailSize}";
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
