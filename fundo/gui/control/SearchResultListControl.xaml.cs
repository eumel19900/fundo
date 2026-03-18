using fundo.core.Search;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace fundo.gui.control
{
    public sealed partial class SearchResultListControl : UserControl
    {
        public SearchResultListControl()
        {
            InitializeComponent();
        }

        public object ItemsSource
        {
            get => SearchResultListView.ItemsSource;
            set => SearchResultListView.ItemsSource = value;
        }

        private async void SearchResultListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await OpenSelectedFiles();
        }

        private List<SearchResultItem> GetSelectedSearchResultItems()
        {
            if (SearchResultListView.SelectionMode == ListViewSelectionMode.Single)
            {
                return SearchResultListView.SelectedItem is SearchResultItem selectedItem
                    ? new List<SearchResultItem> { selectedItem }
                    : new List<SearchResultItem>();
            }

            return SearchResultListView.SelectedItems
                .OfType<SearchResultItem>()
                .ToList();
        }

        private void SearchResultMenuFlyout_Opening(object sender, object e)
        {
            UpdateSearchResultMenuItems();
        }

        private void SearchResultListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject originalSource)
            {
                return;
            }

            DependencyObject? current = originalSource;
            while (current != null)
            {
                if (current is FrameworkElement element && element.DataContext is SearchResultItem searchResultItem)
                {
                    if (SearchResultListView.SelectionMode == ListViewSelectionMode.Single)
                    {
                        SearchResultListView.SelectedItem = searchResultItem;
                    }
                    else if (!SearchResultListView.SelectedItems.Contains(searchResultItem))
                    {
                        SearchResultListView.SelectedItems.Clear();
                        SearchResultListView.SelectedItems.Add(searchResultItem);
                    }

                    UpdateSearchResultMenuItems();
                    return;
                }

                current = VisualTreeHelper.GetParent(current);
            }
            UpdateSearchResultMenuItems();
        }

        private void UpdateSearchResultMenuItems()
        {
            bool hasSelection = GetSelectedSearchResultItems().Count > 0;
            if (SearchResultListView.ContextFlyout is MenuFlyout menuFlyout)
            {
                if (menuFlyout.Items.Count > 0 && menuFlyout.Items[0] is MenuFlyoutItem openFileItem)
                {
                    openFileItem.IsEnabled = hasSelection;
                }

                if (menuFlyout.Items.Count > 1 && menuFlyout.Items[1] is MenuFlyoutItem locateFileItem)
                {
                    locateFileItem.IsEnabled = hasSelection;
                }

                if (menuFlyout.Items.Count > 2 && menuFlyout.Items[2] is MenuFlyoutItem copyFileItem)
                {
                    copyFileItem.IsEnabled = hasSelection;
                }
            }

            ToggleSelectionModeMenuFlyoutItem.Text = SearchResultListView.SelectionMode == ListViewSelectionMode.Multiple
                ? "Enable single-selection"
                : "Enable multi-selection";
        }

        private async void OpenFileMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await OpenSelectedFiles();
        }

        private async void LocateFileMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await LocateSelectedFiles();
        }

        private async void CopyFileMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await CopySelectedFilesToClipboard();
        }

        private void EnableMultiSelectionMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            SearchResultItem? firstSelectedItem = GetSelectedSearchResultItems().FirstOrDefault();

            SearchResultListView.SelectionMode = SearchResultListView.SelectionMode == ListViewSelectionMode.Multiple
                ? ListViewSelectionMode.Single
                : ListViewSelectionMode.Multiple;

            if (SearchResultListView.SelectionMode == ListViewSelectionMode.Single)
            {
                SearchResultListView.SelectedItem = firstSelectedItem;
            }

            UpdateSearchResultMenuItems();
        }

        private async Task OpenSelectedFiles()
        {
            List<SearchResultItem> selectedItems = GetSelectedSearchResultItems();
            if (selectedItems.Count == 0)
            {
                return;
            }

            try
            {
                foreach (SearchResultItem selectedItem in selectedItems)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = selectedItem.FileInfo.FullName,
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
            List<string> directories = GetSelectedSearchResultItems()
                .Select(item => item.FileInfo.DirectoryName)
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            if (directories.Count == 0)
            {
                return;
            }

            try
            {
                foreach (string directory in directories)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = directory,
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
            List<SearchResultItem> selectedItems = GetSelectedSearchResultItems();
            if (selectedItems.Count == 0)
            {
                return;
            }

            try
            {
                List<IStorageItem> files = new List<IStorageItem>();
                foreach (SearchResultItem selectedItem in selectedItems)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(selectedItem.FileInfo.FullName);
                    files.Add(file);
                }

                DataPackage dataPackage = new DataPackage();
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
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }
    }
}
