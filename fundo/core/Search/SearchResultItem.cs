using fundo.gui.tool;
using System;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace fundo.core.Search
{
    internal class SearchResultItem : INotifyPropertyChanged
    {
        private String fileName;
        private String path;
        private long fileSize;
        private DateTime fileDate;
        private FileInfo fileInfo;
        private Icon? fileIcon;
        private ImageSource? fileImage;

        public string FileName { get => fileName; set => fileName = value; }
        public long FileSize { get => fileSize; set => fileSize = value; }
        public DateTime FileDate { get => fileDate; set => fileDate = value; }
        public FileInfo FileInfo { get => fileInfo; set => fileInfo = value; }
        public string Path { get => path; set => path = value; }
        public string FileSizeString => FileSizeStringHelper.ToHumanReadable(fileSize);
        // Icon for the file (may be null). This is populated from the system registered icon for the file.
        public Icon? FileIcon { get => fileIcon; }
        // WinUI ImageSource for binding in XAML (may be null until loaded)
        public ImageSource? FileImage { get => fileImage; private set { fileImage = value; OnPropertyChanged(nameof(FileImage)); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public SearchResultItem()
        {

        }


        public SearchResultItem(FileInfo fileInfo)
        {
            this.fileName = fileInfo.Name;
            this.path = fileInfo.FullName;
            this.fileSize = fileInfo.Length;
            this.fileDate = fileInfo.CreationTime;
            this.fileInfo = fileInfo;
            // do not eagerly load icons here to avoid blocking. Start lazy async load of image bytes instead.
            _ = LoadImageAsync(fileInfo.FullName);
        }

        private async Task LoadImageAsync(string filePath)
        {
            try
            {
                var pngBytes = await fundo.gui.tool.FileIconLoader.GetPngBytesAsync(filePath, false).ConfigureAwait(false);
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
