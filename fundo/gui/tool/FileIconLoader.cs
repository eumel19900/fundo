using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace fundo.gui.tool
{
    /// <summary>
    /// Lädt das in Windows registrierte Dateisymbol (Icon) für einen Dateinamen oder eine Dateiendung.
    /// Liefert ein <see cref="System.Drawing.Icon"/> (kopiert) oder IntPtr zu einem HICON.
    /// </summary>
    internal static class FileIconLoader
    {
        // cache icons per normalized extension (e.g. ".txt")
        private static readonly ConcurrentDictionary<string, Icon> _iconCache = new();
        // cache generated PNG bytes per normalized extension to avoid repeated conversion
        private static readonly ConcurrentDictionary<string, byte[]> _pngCache = new();

        // SHGetFileInfo flags
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        // file attribute for use with SHGFI_USEFILEATTRIBUTES
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Gibt das Icon (kopiert) für die angegebene Datei oder Dateiendung zurück.
        /// Wenn die Datei nicht existiert, wird das registrierte Icon anhand der Endung ermittelt.
        /// Der zurückgegebene <see cref="Icon"/> muss vom Aufrufer entsorgt werden.
        /// </summary>
        /// <param name="filePathOrExtension">Pfad zu einer Datei oder nur die Dateiendung (z. B. ".txt").</param>
        /// <param name="smallIcon">True => kleines Symbol, false => großes Symbol.</param>
        /// <returns>Ein neues <see cref="Icon"/>-Objekt oder null, falls kein Icon gefunden wurde.</returns>
        public static Icon? GetIcon(string filePathOrExtension, bool smallIcon = true)
        {
            if (string.IsNullOrEmpty(filePathOrExtension)) return null;
            // determine cache key: prefer extension (lowercase, with leading dot)
            string key = NormalizeKey(filePathOrExtension);

            if (_iconCache.TryGetValue(key, out var cachedIcon))
            {
                // return a clone so the caller may dispose it independently
                return (Icon)cachedIcon.Clone();
            }

            uint flags = SHGFI_ICON | (smallIcon ? SHGFI_SMALLICON : SHGFI_LARGEICON);
            uint attributes = 0;

            // If the file does not exist, ask SHGetFileInfo to use the extension's registration
            if (!File.Exists(filePathOrExtension))
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                attributes = FILE_ATTRIBUTE_NORMAL;
            }

            var shfi = new SHFILEINFO();
            IntPtr result = SHGetFileInfo(filePathOrExtension, attributes, out shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);

            if (shfi.hIcon == IntPtr.Zero) return null;

            try
            {
                // Create an Icon object from the native handle.
                using var iconFromHandle = Icon.FromHandle(shfi.hIcon);
                var iconForCaller = (Icon)iconFromHandle.Clone();

                // store a clone in the cache for future callers
                try
                {
                    var cacheIcon = (Icon)iconForCaller.Clone();
                    _iconCache.TryAdd(key, cacheIcon);
                }
                catch
                {
                    // ignore cache insertion failures
                }

                return iconForCaller;
            }
            finally
            {
                // Destroy the native icon handle.
                DestroyIcon(shfi.hIcon);
            }
        }

        private static string NormalizeKey(string filePathOrExtension)
        {
            try
            {
                if (filePathOrExtension.StartsWith(".") && !filePathOrExtension.Contains(Path.DirectorySeparatorChar) && !filePathOrExtension.Contains(Path.AltDirectorySeparatorChar))
                {
                    return filePathOrExtension.ToLowerInvariant();
                }
                else
                {
                    var ext = Path.GetExtension(filePathOrExtension);
                    return string.IsNullOrEmpty(ext) ? filePathOrExtension.ToLowerInvariant() : ext.ToLowerInvariant();
                }
            }
            catch
            {
                return filePathOrExtension.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Returns PNG-encoded bytes for the icon associated with the given file/extension.
        /// Uses internal caches so conversion happens at most once per extension.
        /// </summary>
        public static async Task<byte[]?> GetPngBytesAsync(string filePathOrExtension, bool smallIcon = true)
        {
            if (string.IsNullOrEmpty(filePathOrExtension)) return null;

            var key = NormalizeKey(filePathOrExtension);
            if (_pngCache.TryGetValue(key, out var cachedBytes))
            {
                return cachedBytes;
            }

            return await Task.Run(() =>
            {
                Icon? icon = null;
                try
                {
                    icon = GetIcon(filePathOrExtension, smallIcon);
                    if (icon == null) return null;

                    using var bmp = icon.ToBitmap();
                    using var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var bytes = ms.ToArray();

                    // cache bytes
                    try { _pngCache.TryAdd(key, bytes); } catch { }

                    return bytes;
                }
                finally
                {
                    icon?.Dispose();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Liefert den nativen HICON (IntPtr) für die angegebene Datei/Endung.
        /// Der Caller ist verantwortlich, das Icon mit DestroyIcon freizugeben.
        /// </summary>
        /// <param name="filePathOrExtension">Pfad zu einer Datei oder Dateiendung (z. B. ".txt").</param>
        /// <param name="smallIcon">True => kleines Symbol, false => großes Symbol.</param>
        /// <returns>HICON (IntPtr) oder IntPtr.Zero.</returns>
        public static IntPtr GetIconHandle(string filePathOrExtension, bool smallIcon = true)
        {
            if (string.IsNullOrEmpty(filePathOrExtension)) return IntPtr.Zero;

            uint flags = SHGFI_ICON | (smallIcon ? SHGFI_SMALLICON : SHGFI_LARGEICON);
            uint attributes = 0;

            if (!File.Exists(filePathOrExtension))
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                attributes = FILE_ATTRIBUTE_NORMAL;
            }

            var shfi = new SHFILEINFO();
            IntPtr result = SHGetFileInfo(filePathOrExtension, attributes, out shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);

            return shfi.hIcon; // caller must call DestroyIcon on this handle
        }

        /// <summary>
        /// Liefert ein WinUI <see cref="ImageSource"/> (BitmapImage) aus dem Dateisymbol.
        /// Diese Methode ist asynchron, da sie den Bitmap-Stream in ein IRandomAccessStream konvertiert
        /// und <see cref="BitmapImage.SetSourceAsync(IRandomAccessStream)"/> aufruft.
        /// </summary>
        public static async Task<ImageSource?> GetImageSourceAsync(string filePathOrExtension, bool smallIcon = true)
        {
            var icon = GetIcon(filePathOrExtension, smallIcon);
            if (icon is null) return null;

            try
            {
                using var bmp = icon.ToBitmap();
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                IRandomAccessStream ras = ms.AsRandomAccessStream();
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(ras);
                return bitmapImage;
            }
            catch
            {
                return null;
            }
            finally
            {
                // icon was created by GetIcon and must be disposed by us
                icon.Dispose();
            }
        }
    }
}
