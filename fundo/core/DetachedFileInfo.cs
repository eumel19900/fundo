using fundo.core.Persistence.Entity;
using fundo.tool;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace fundo.core
{
    internal class DetachedFileInfo : INotifyPropertyChanged
    {
        private bool _ioPropertiesInitialized = true;
        private bool _initializeIoPropertiesOnDemand;
        private bool _exists;
        private DateTime _creationTime;
        private DateTime _creationTimeUtc;
        private DateTime _lastAccessTime;
        private DateTime _lastAccessTimeUtc;
        private DateTime _lastWriteTime;
        private DateTime _lastWriteTimeUtc;
        private FileAttributes _attributes;
        private UnixFileMode _unixFileMode;
        private long _length;
        private bool _isReadOnly;
        private string _linkTarget;
        private ImageSource? _fileImage;
        private bool _imageLoadStarted;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // FileSystemInfo properties
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public bool Exists
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _exists;
            }
            set => _exists = value;
        }
        public DateTime CreationTime
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _creationTime;
            }
            set => _creationTime = value;
        }
        public DateTime CreationTimeUtc
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _creationTimeUtc;
            }
            set => _creationTimeUtc = value;
        }
        public DateTime LastAccessTime
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _lastAccessTime;
            }
            set => _lastAccessTime = value;
        }
        public DateTime LastAccessTimeUtc
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _lastAccessTimeUtc;
            }
            set => _lastAccessTimeUtc = value;
        }
        public DateTime LastWriteTime
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _lastWriteTime;
            }
            set => _lastWriteTime = value;
        }
        public DateTime LastWriteTimeUtc
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _lastWriteTimeUtc;
            }
            set => _lastWriteTimeUtc = value;
        }
        public FileAttributes Attributes
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _attributes;
            }
            set => _attributes = value;
        }
        public UnixFileMode UnixFileMode
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _unixFileMode;
            }
            set => _unixFileMode = value;
        }

        public ImageSource? FileImage
        {
            get
            {
                if (!_imageLoadStarted && !string.IsNullOrEmpty(FullName))
                {
                    _imageLoadStarted = true;
                    _ = LoadImageAsync(FullName);
                }
                return _fileImage;
            }
            private set
            {
                _fileImage = value;
                OnPropertyChanged(nameof(FileImage));
            }
        }

        public string FileSizeString => FileSizeStringHelper.ToHumanReadable(Length);

        // FileInfo properties
        public string DirectoryName { get; set; }
        public long Length
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _length;
            }
            set => _length = value;
        }
        public bool IsReadOnly
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _isReadOnly;
            }
            set => _isReadOnly = value;
        }
        public string LinkTarget
        {
            get
            {
                EnsureIoPropertiesInitialized();
                return _linkTarget;
            }
            set => _linkTarget = value;
        }

        public DetachedFileInfo()
        {
        }

        public DetachedFileInfo(FileInfo fileInfo, bool includeIoProperties = true)
        {
            Name = fileInfo.Name;
            FullName = fileInfo.FullName;
            Extension = fileInfo.Extension;
            DirectoryName = fileInfo.DirectoryName;

            if (!includeIoProperties)
            {
                _ioPropertiesInitialized = false;
                _initializeIoPropertiesOnDemand = true;
                return;
            }

            SetIoProperties(fileInfo);
        }

        public DetachedFileInfo(FileEntity fileEntity)
        {
            Name = fileEntity.FileName;
            FullName = fileEntity.Path;
            Extension = fileEntity.FileType;
            _creationTime = fileEntity.CreationTime;
            _creationTimeUtc = fileEntity.CreationTime.ToUniversalTime();
            _lastAccessTime = fileEntity.LastAccessTime;
            _lastAccessTimeUtc = fileEntity.LastAccessTime.ToUniversalTime();
            _lastWriteTime = fileEntity.ModifiedTime;
            _lastWriteTimeUtc = fileEntity.ModifiedTime.ToUniversalTime();
            _attributes = FileAttributeHelper.ToSystemFileAttributes(fileEntity.FileAttributes);
            DirectoryName = Path.GetDirectoryName(fileEntity.Path);
            _length = fileEntity.FileSize;
            _isReadOnly = FileAttributeHelper.HasAttribute(fileEntity.FileAttributes, FileAttribute.Readonly);
            _ioPropertiesInitialized = true;
        }

        private void EnsureIoPropertiesInitialized()
        {
            if (_ioPropertiesInitialized || !_initializeIoPropertiesOnDemand)
            {
                return;
            }

            if (string.IsNullOrEmpty(FullName))
            {
                _ioPropertiesInitialized = true;
                _initializeIoPropertiesOnDemand = false;
                return;
            }

            SetIoProperties(new FileInfo(FullName));
        }

        private void SetIoProperties(FileInfo fileInfo)
        {
            _exists = fileInfo.Exists;

            if (_exists)
            {
                _creationTime = fileInfo.CreationTime;
                _creationTimeUtc = fileInfo.CreationTimeUtc;
                _lastAccessTime = fileInfo.LastAccessTime;
                _lastAccessTimeUtc = fileInfo.LastAccessTimeUtc;
                _lastWriteTime = fileInfo.LastWriteTime;
                _lastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                _attributes = fileInfo.Attributes;
                _length = fileInfo.Length;
                _isReadOnly = fileInfo.IsReadOnly;
                _linkTarget = fileInfo.LinkTarget;

                if (!OperatingSystem.IsWindows())
                {
                    _unixFileMode = fileInfo.UnixFileMode;
                }
            }

            _ioPropertiesInitialized = true;
            _initializeIoPropertiesOnDemand = false;
        }

        private async Task LoadImageAsync(string filePath)
        {
            try
            {
                var pngBytes = await FileIconLoader.GetPngBytesAsync(filePath, false).ConfigureAwait(false);
                if (pngBytes == null || pngBytes.Length == 0) return;

                var disp = App.MainWindowInstance?.DispatcherQueue;
                if (disp != null)
                {
                    disp.TryEnqueue(async () =>
                    {
                        try
                        {
                            var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                            using var ms = new MemoryStream(pngBytes);
                            var ras = ms.AsRandomAccessStream();
                            await bmp.SetSourceAsync(ras);
                            FileImage = bmp;
                        }
                        catch
                        {
                            // ignore UI-thread image creation failures
                        }
                    });
                }
                else
                {
                    try
                    {
                        var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                        using var ms = new MemoryStream(pngBytes);
                        var ras = ms.AsRandomAccessStream();
                        await bmp.SetSourceAsync(ras);
                        FileImage = bmp;
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                // ignore image load failures
            }
        }
    }
}
