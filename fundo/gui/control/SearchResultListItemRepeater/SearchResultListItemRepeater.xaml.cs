using fundo.core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

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

        public SearchResultListItemRepeater()
        {
            InitializeComponent();
            _dataProvider.ViewChanged += DataProvider_ViewChanged;
            _dataView = new SearchResultDataView(_dataProvider);
            Repeater.ItemsSource = _dataView;
            Repeater.ElementPrepared += Repeater_ElementPrepared;
            Repeater.ElementClearing += Repeater_ElementClearing;
        }

        internal SearchResultDataProvider DataProvider => _dataProvider;

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
        }

        private void Repeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
        {
            SetElementBackground(args.Element, false);
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
