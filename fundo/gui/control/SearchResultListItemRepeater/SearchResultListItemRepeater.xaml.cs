using fundo.core;
using fundo.tool;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;

namespace fundo.gui.control
{
    public sealed partial class SearchResultListItemRepeater : UserControl
    {
        private readonly SearchResultDataProvider _dataProvider = new();
        private readonly SearchResultDataView _dataView;
        private readonly HashSet<int> _selectedIndices = new();
        private HashSet<int> _visuallySelectedIndices = new();
        private bool _multiSelectMode = false;
        private Brush? _selectedBrush;
        private static readonly SolidColorBrush TransparentBrush = new(Microsoft.UI.Colors.Transparent);
        private DispatcherQueueTimer? _filterDebounceTimer;

        private bool _thumbnailMode = false;
        private readonly ThumbnailGenerator _thumbnailGenerator = new();

        private const int MinThumbnailImageSize = 64;
        private const int MaxThumbnailImageSize = 320;
        private const int ThumbnailImageSizeStep = 16;
        private const int ThumbnailItemPadding = 20;
        private int _thumbnailImageSize = 120;
        private bool _suppressScrollResizeHandling;
        private bool _suppressNativeZoomHandling;
        private double _lastKnownHorizontalOffset;
        private double _lastKnownVerticalOffset;
        private float _lastKnownZoomFactor = 1f;

        // DataTemplates created in code to switch between list and tile views
        private DataTemplate? _listTemplate;
        private DataTemplate? _tileTemplate;

        public SearchResultListItemRepeater()
        {
            InitializeComponent();
            _dataProvider.ViewChanged += DataProvider_ViewChanged;
            _dataView = new SearchResultDataView(_dataProvider);
            Repeater.ItemsSource = _dataView;
            Repeater.ElementPrepared += Repeater_ElementPrepared;
            Repeater.ElementClearing += Repeater_ElementClearing;
            RepeaterScrollViewer.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(RepeaterScrollViewer_PointerWheelChanged), true);
            RepeaterScrollViewer.AddHandler(KeyDownEvent, new KeyEventHandler(RepeaterScrollViewer_KeyDown), true);
            RepeaterScrollViewer.ViewChanging += RepeaterScrollViewer_ViewChanging;
            CreateTemplates();
            ApplyViewMode();
        }

        internal SearchResultDataProvider DataProvider => _dataProvider;

        private static string CreateFileInfoToolTipXaml()
        {
            return @"
                        <ToolTipService.ToolTip>
                            <ToolTip>
                                <ScrollViewer MaxHeight=""500"" VerticalScrollBarVisibility=""Auto"">
                                    <TextBlock MaxWidth=""700""
                                               Text=""{Binding ToolTipText}""
                                               TextWrapping=""WrapWholeWords""/>
                                </ScrollViewer>
                            </ToolTip>
                        </ToolTipService.ToolTip>";
        }

        private void CreateTemplates()
        {
            string toolTipXaml = CreateFileInfoToolTipXaml();
            int thumbnailItemWidth = _thumbnailImageSize + ThumbnailItemPadding;
            int thumbnailItemHeight = _thumbnailImageSize + 40;

            _listTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                    <Border Padding=""8,4"" CornerRadius=""4"" Margin=""0,1"" Background=""Transparent"">
" + toolTipXaml + @"
                        <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"">
                            <Image Source=""{Binding FileImage}""
                                   Width=""32"" Height=""32"" Margin=""0,0,8,0""/>
                            <StackPanel Orientation=""Vertical"">
                                <TextBlock Text=""{Binding Name}"" FontSize=""18""
                                           Foreground=""{ThemeResource TextFillColorPrimaryBrush}""/>
                                <TextBlock Text=""{Binding FullName}"" FontSize=""10""
                                           Foreground=""{ThemeResource TextFillColorSecondaryBrush}""/>
                                <TextBlock Text=""{Binding CreationTime}"" FontSize=""10""
                                           Foreground=""{ThemeResource TextFillColorSecondaryBrush}""/>
                                <TextBlock Text=""{Binding FileSizeString}"" FontSize=""10""
                                           Foreground=""{ThemeResource TextFillColorSecondaryBrush}""/>
                            </StackPanel>
                        </StackPanel>
                    </Border>
                </DataTemplate>");

            _tileTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                    <Border Padding=""4"" CornerRadius=""4"" Margin=""2"" Background=""Transparent""
                            Width=""" + thumbnailItemWidth + @""" Height=""" + thumbnailItemHeight + @""">
" + toolTipXaml + @"
                        <StackPanel HorizontalAlignment=""Center"">
                            <Image Source=""{Binding FileImage}""
                                   Width=""" + _thumbnailImageSize + @""" Height=""" + _thumbnailImageSize + @""" Stretch=""Uniform""/>
                            <TextBlock Text=""{Binding Name}"" FontSize=""11""
                                       TextTrimming=""CharacterEllipsis"" MaxWidth=""130""
                                       HorizontalAlignment=""Center"" Margin=""0,4,0,0""
                                       Foreground=""{ThemeResource TextFillColorPrimaryBrush}""/>
                        </StackPanel>
                    </Border>
                </DataTemplate>");
        }

        private void ApplyViewMode()
        {
            if (_thumbnailMode)
            {
                int thumbnailItemWidth = _thumbnailImageSize + ThumbnailItemPadding;
                int thumbnailItemHeight = _thumbnailImageSize + 40;

                Repeater.Layout = new UniformGridLayout
                {
                    MinItemWidth = thumbnailItemWidth,
                    MinItemHeight = thumbnailItemHeight,
                    MinColumnSpacing = 4,
                    MinRowSpacing = 4,
                    ItemsStretch = UniformGridLayoutItemsStretch.None
                };
                Repeater.ItemTemplate = _tileTemplate;
            }
            else
            {
                Repeater.Layout = new StackLayout { Spacing = 2 };
                Repeater.ItemTemplate = _listTemplate;
            }
        }


        internal void SetItems(IReadOnlyList<DetachedFileInfo> items)
        {
            _selectedIndices.Clear();
            _visuallySelectedIndices.Clear();
            _dataProvider.SetItems(items);
        }

        internal void Clear()
        {
            _selectedIndices.Clear();
            _visuallySelectedIndices.Clear();
            _dataProvider.Clear();
        }

        private Brush GetSelectedBrush()
        {
            if (_selectedBrush == null)
            {
                if (Application.Current.Resources.TryGetValue("SubtleFillColorSecondaryBrush", out var brush) && brush is Brush b)
                    _selectedBrush = b;
                else
                    _selectedBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0x30, 0x00, 0x78, 0xD4));
            }
            return _selectedBrush;
        }

        private void DataProvider_ViewChanged(object? sender, EventArgs e)
        {
            _selectedIndices.Clear();
            _visuallySelectedIndices.Clear();
        }

        #region Selection

        private void SelectItem(int viewIndex, bool rightTap)
        {
            if (viewIndex < 0 || viewIndex >= _dataProvider.Count) return;

            if (_multiSelectMode)
            {
                if (rightTap)
                {
                    if (!_selectedIndices.Contains(viewIndex))
                    {
                        _selectedIndices.Clear();
                        _selectedIndices.Add(viewIndex);
                    }
                }
                else
                {
                    if (_selectedIndices.Contains(viewIndex))
                        _selectedIndices.Remove(viewIndex);
                    else
                        _selectedIndices.Add(viewIndex);
                }
            }
            else
            {
                _selectedIndices.Clear();
                _selectedIndices.Add(viewIndex);
            }

            UpdateSelectionVisuals();
        }

        private List<DetachedFileInfo> GetSelectedItems()
        {
            return _selectedIndices
                .Where(i => i >= 0 && i < _dataProvider.Count)
                .Select(i => _dataProvider.GetAt(i))
                .ToList();
        }

        private void UpdateSelectionVisuals()
        {
            foreach (int index in _visuallySelectedIndices)
            {
                if (!_selectedIndices.Contains(index))
                {
                    var element = Repeater.TryGetElement(index);
                    if (element != null) SetElementBackground(element, false);
                }
            }

            foreach (int index in _selectedIndices)
            {
                if (!_visuallySelectedIndices.Contains(index))
                {
                    var element = Repeater.TryGetElement(index);
                    if (element != null) SetElementBackground(element, true);
                }
            }

            _visuallySelectedIndices = new HashSet<int>(_selectedIndices);
        }

        private void SetElementBackground(UIElement element, bool selected)
        {
            if (element is Border border)
            {
                border.Background = selected ? GetSelectedBrush() : TransparentBrush;
            }
        }

        #endregion

        #region Element Recycling

        private void Repeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            SetElementBackground(args.Element, _selectedIndices.Contains(args.Index));

            if (_thumbnailMode && args.Index >= 0 && args.Index < _dataProvider.Count)
            {
                var item = _dataProvider.GetAt(args.Index);
                var ext = (Path.GetExtension(item.FullName) ?? "").ToLowerInvariant();
                if (ThumbnailGenerator.IsImageFile(ext))
                {
                    LoadThumbnailForElement(args.Element, item);
                }
            }
        }

        private void Repeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
        {
            SetElementBackground(args.Element, false);

            if (_thumbnailMode)
            {
                var image = FindThumbnailImage(args.Element);
                if (image != null && image.DataContext is DetachedFileInfo item)
                {
                    var ext = (Path.GetExtension(item.FullName) ?? "").ToLowerInvariant();
                    if (ThumbnailGenerator.IsImageFile(ext))
                    {
                        // Cancel the pending background request to avoid unnecessary CPU work
                        _thumbnailGenerator.CancelPending(item.FullName, _thumbnailImageSize);
                        // Restore the FileImage binding that was replaced by the thumbnail
                        image.SetBinding(Image.SourceProperty, new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath("FileImage") });
                    }
                }
            }
        }

        private void LoadThumbnailForElement(UIElement element, DetachedFileInfo item)
        {
            var dispatcher = DispatcherQueue;
            if (dispatcher == null) return;

            int requestedThumbnailSize = _thumbnailImageSize;
            _thumbnailGenerator.RequestThumbnail(item.FullName, requestedThumbnailSize, dispatcher, thumbnail =>
            {
                if (!_thumbnailMode || requestedThumbnailSize != _thumbnailImageSize)
                {
                    return;
                }

                var image = FindThumbnailImage(element);
                if (image != null && ReferenceEquals(image.DataContext, item))
                {
                    image.Source = thumbnail;
                }
            });
        }

        private static Image? FindThumbnailImage(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Image img)
                    return img;
                var result = FindThumbnailImage(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void RepeaterScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!_thumbnailMode || !IsControlKeyPressed())
            {
                return;
            }

            int delta = e.GetCurrentPoint(RepeaterScrollViewer).Properties.MouseWheelDelta;
            if (delta == 0)
            {
                return;
            }

            int newSize = _thumbnailImageSize + (delta > 0 ? ThumbnailImageSizeStep : -ThumbnailImageSizeStep);
            ApplyThumbnailSizeChange(newSize);
            e.Handled = true;
        }

        private void RepeaterScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            if (_suppressScrollResizeHandling || _suppressNativeZoomHandling || !_thumbnailMode)
            {
                _lastKnownHorizontalOffset = e.NextView.HorizontalOffset;
                _lastKnownVerticalOffset = e.NextView.VerticalOffset;
                _lastKnownZoomFactor = e.NextView.ZoomFactor;
                return;
            }

            float zoomDelta = e.NextView.ZoomFactor - _lastKnownZoomFactor;
            if (Math.Abs(zoomDelta) > 0.001f)
            {
                int pinchNewSize = _thumbnailImageSize + (zoomDelta > 0 ? ThumbnailImageSizeStep : -ThumbnailImageSizeStep);
                bool pinchChanged = ApplyThumbnailSizeChange(pinchNewSize);

                _suppressNativeZoomHandling = true;
                RepeaterScrollViewer.ChangeView(_lastKnownHorizontalOffset, _lastKnownVerticalOffset, 1f, true);
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    _suppressNativeZoomHandling = false;
                    _lastKnownZoomFactor = 1f;
                });

                if (pinchChanged)
                {
                    return;
                }
            }

            if (!IsControlKeyPressed())
            {
                _lastKnownHorizontalOffset = e.NextView.HorizontalOffset;
                _lastKnownVerticalOffset = e.NextView.VerticalOffset;
                _lastKnownZoomFactor = e.NextView.ZoomFactor;
                return;
            }

            double delta = e.NextView.VerticalOffset - _lastKnownVerticalOffset;
            if (Math.Abs(delta) < 1)
            {
                _lastKnownHorizontalOffset = e.NextView.HorizontalOffset;
                _lastKnownVerticalOffset = e.NextView.VerticalOffset;
                _lastKnownZoomFactor = e.NextView.ZoomFactor;
                return;
            }

            int direction = delta < 0 ? 1 : -1;
            int newSize = _thumbnailImageSize + (direction * ThumbnailImageSizeStep);
            bool changed = ApplyThumbnailSizeChange(newSize);

            if (changed)
            {
                _suppressScrollResizeHandling = true;
                RepeaterScrollViewer.ChangeView(_lastKnownHorizontalOffset, _lastKnownVerticalOffset, 1f, true);
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    _suppressScrollResizeHandling = false;
                    _lastKnownZoomFactor = 1f;
                });
            }
            else
            {
                _lastKnownHorizontalOffset = e.NextView.HorizontalOffset;
                _lastKnownVerticalOffset = e.NextView.VerticalOffset;
                _lastKnownZoomFactor = e.NextView.ZoomFactor;
            }
        }

        private void RepeaterScrollViewer_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_thumbnailMode || !IsControlKeyPressed())
            {
                return;
            }

            int? direction = e.Key switch
            {
                Windows.System.VirtualKey.Add => 1,
                (Windows.System.VirtualKey)187 => 1,
                Windows.System.VirtualKey.Subtract => -1,
                (Windows.System.VirtualKey)189 => -1,
                _ => null
            };

            if (!direction.HasValue)
            {
                return;
            }

            ApplyThumbnailSizeChange(_thumbnailImageSize + (direction.Value * ThumbnailImageSizeStep));
            e.Handled = true;
        }

        private bool ApplyThumbnailSizeChange(int newSize)
        {
            newSize = Math.Clamp(newSize, MinThumbnailImageSize, MaxThumbnailImageSize);
            if (newSize == _thumbnailImageSize)
            {
                return false;
            }

            _thumbnailImageSize = newSize;
            _thumbnailGenerator.ClearCache();
            CreateTemplates();
            ApplyViewMode();
            Repeater.ItemsSource = null;
            Repeater.ItemsSource = _dataView;
            return true;
        }

        private static bool IsControlKeyPressed()
        {
            CoreVirtualKeyStates keyState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
            return (keyState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        #endregion

        #region Input Events

        private int GetItemIndexFromElement(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is UIElement uiElement)
                {
                    int index = Repeater.GetElementIndex(uiElement);
                    if (index >= 0) return index;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return -1;
        }

        private void ItemsRepeater_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            int index = GetItemIndexFromElement(source);
            if (index >= 0) SelectItem(index, false);
        }

        private void ItemsRepeater_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            int index = GetItemIndexFromElement(source);
            if (index >= 0) SelectItem(index, true);
        }

        private async void ItemsRepeater_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            int index = GetItemIndexFromElement(source);
            if (index >= 0)
            {
                SelectItem(index, false);
                await OpenSelectedFiles();
            }
        }

        #endregion

        #region Context Menu

        private void ContextMenuFlyout_Opening(object sender, object e)
        {
            bool hasSelection = _selectedIndices.Count > 0;
            OpenFileMenuItem.IsEnabled = hasSelection;
            LocateFileMenuItem.IsEnabled = hasSelection;
            CopyFileMenuItem.IsEnabled = hasSelection;

            ToggleSelectionModeMenuItem.Text = _multiSelectMode
                ? "Enable single-selection"
                : "Enable multi-selection";

            bool hasFilter = !string.IsNullOrEmpty(_dataProvider.FileNameFilter);
            FilterByNameMenuItem.Text = hasFilter
                ? $"Filter: \"{_dataProvider.FileNameFilter}\""
                : "Filter by file name...";
            ClearFilterMenuItem.IsEnabled = hasFilter;

            string arrow = _dataProvider.SortDirection == SearchResultSortDirection.Ascending ? " \u2191" : " \u2193";
            SortByNameMenuItem.Text = "Sort by name" + (_dataProvider.SortField == SearchResultSortField.FileName ? arrow : "");
            SortByDirectoryMenuItem.Text = "Sort by directory" + (_dataProvider.SortField == SearchResultSortField.Directory ? arrow : "");
            SortBySizeMenuItem.Text = "Sort by size" + (_dataProvider.SortField == SearchResultSortField.FileSize ? arrow : "");
            SortByDateMenuItem.Text = "Sort by date" + (_dataProvider.SortField == SearchResultSortField.FileDate ? arrow : "");
            SortByTypeMenuItem.Text = "Sort by type" + (_dataProvider.SortField == SearchResultSortField.FileType ? arrow : "");

            ToggleViewModeMenuItem.Text = _thumbnailMode ? "List view" : "Thumbnail view";
        }

        private async void OpenFileMenuItem_Click(object sender, RoutedEventArgs e) => await OpenSelectedFiles();
        private async void LocateFileMenuItem_Click(object sender, RoutedEventArgs e) => await LocateSelectedFiles();
        private async void CopyFileMenuItem_Click(object sender, RoutedEventArgs e) => await CopySelectedFilesToClipboard();

        private void ToggleSelectionModeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _multiSelectMode = !_multiSelectMode;
            if (!_multiSelectMode && _selectedIndices.Count > 1)
            {
                int first = _selectedIndices.Min();
                _selectedIndices.Clear();
                _selectedIndices.Add(first);
                UpdateSelectionVisuals();
            }
        }

        private void SortByNameMenuItem_Click(object sender, RoutedEventArgs e) => _dataProvider.SetSort(SearchResultSortField.FileName);
        private void SortByDirectoryMenuItem_Click(object sender, RoutedEventArgs e) => _dataProvider.SetSort(SearchResultSortField.Directory);
        private void SortBySizeMenuItem_Click(object sender, RoutedEventArgs e) => _dataProvider.SetSort(SearchResultSortField.FileSize);
        private void SortByDateMenuItem_Click(object sender, RoutedEventArgs e) => _dataProvider.SetSort(SearchResultSortField.FileDate);
        private void SortByTypeMenuItem_Click(object sender, RoutedEventArgs e) => _dataProvider.SetSort(SearchResultSortField.FileType);

        private void ToggleViewModeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _thumbnailMode = !_thumbnailMode;
            ApplyViewMode();

            // Force re-render by resetting the data source
            Repeater.ItemsSource = null;
            Repeater.ItemsSource = _dataView;
        }

        private void FilterByNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Text = _dataProvider.FileNameFilter;

            var popupChild = (FrameworkElement)FilterPopup.Child;
            popupChild.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

            FilterPopup.HorizontalOffset = ActualWidth - popupChild.DesiredSize.Width - 20;
            FilterPopup.VerticalOffset = ActualHeight - popupChild.DesiredSize.Height - 20;
            FilterPopup.IsOpen = true;
            FilterTextBox.Focus(FocusState.Programmatic);
            FilterTextBox.SelectAll();
        }

        private void ClearFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _dataProvider.SetFileNameFilter(string.Empty);
        }

        #endregion

        #region Filter Popup

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filterDebounceTimer?.Stop();

            _filterDebounceTimer = DispatcherQueue.CreateTimer();
            _filterDebounceTimer.Interval = TimeSpan.FromMilliseconds(250);
            _filterDebounceTimer.IsRepeating = false;
            _filterDebounceTimer.Tick += (_, _) =>
            {
                _dataProvider.SetFileNameFilter(FilterTextBox.Text);
            };
            _filterDebounceTimer.Start();
        }

        private void FilterTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                FilterPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void FilterPopup_Closed(object sender, object e)
        {
            _filterDebounceTimer?.Stop();
            _dataProvider.SetFileNameFilter(FilterTextBox.Text);
        }

        #endregion

        #region File Operations

        private async Task OpenSelectedFiles()
        {
            var items = GetSelectedItems();
            if (items.Count == 0) return;

            try
            {
                foreach (var item in items)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = item.FullName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Unable to open file", ex.Message);
            }
        }

        private async Task LocateSelectedFiles()
        {
            var directories = GetSelectedItems()
                .Select(item => item.DirectoryName)
                .Where(dir => !string.IsNullOrWhiteSpace(dir))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (directories.Count == 0) return;

            try
            {
                foreach (var dir in directories)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dir!,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Unable to locate file", ex.Message);
            }
        }

        private async Task CopySelectedFilesToClipboard()
        {
            var items = GetSelectedItems();
            if (items.Count == 0) return;

            try
            {
                var files = new List<IStorageItem>();
                foreach (var item in items)
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FullName);
                    files.Add(file);
                }

                var dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetStorageItems(files);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Unable to copy file", ex.Message);
            }
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };
            await dialog.ShowAsync();
        }

        #endregion
    }
}
