using fundo.core;
using fundo.core.Persistence;
using fundo.core.Persistence.Filter;
using fundo.core.Search;
using fundo.core.Search.Filter;
using fundo.gui.Job;
using fundo.gui.Job.Jobs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace fundo.gui.page
{
    public sealed partial class SearchPage : Page
    {
        private SearchEngine currentSearchEngine = null;
        private List<DirectoryInfo> rootSearchDirectories = new();
        private List<SearchFilter> filters = new List<SearchFilter>();

        // Filter pages
        private DateFilterPage? dateFilterPage;
        private AttributeFilterPage? attributeFilterPage;
        private FileContentFilterPage? fileContentFilterPage;
        private SizeFilterPage? sizeFilterPage;

        public SearchPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            LocationControl_SelectedDirectoryChanged(null, null);
            ContentFrame.Navigated += ContentFrame_Navigated;
            chooseSearchEngine();
        }

        public void chooseSearchEngine()
        {
            if (Settings.UseIndex)
            {
                currentSearchEngine = new IndexBasedSearchEngine();
                SearchEngineInfoBar.Message =
                    "Fundo is using the index-based searching backend. You may get outdated results. This can be changed in the settings.";
            }
            else
            {
                currentSearchEngine = new NativeSearchEngine();
                SearchEngineInfoBar.Message =
                    "Fundo will search directly in your filesystem. This can be slow. You can change to index based file search in the settings.";
            }

            SearchEngineInfoBar.Title = "Search engine";
            SearchEngineInfoBar.IsOpen = true;

            // nach 10 Sekunden automatisch schließen (nicht blocking)
            _ = AutoCloseSearchEngineInfoBarAsync(TimeSpan.FromSeconds(10));
        }

        private async Task AutoCloseSearchEngineInfoBarAsync(TimeSpan delay)
        {
            try
            {
                await Task.Delay(delay);
                SearchEngineInfoBar.IsOpen = false;
            }
            catch
            {
                // ignorieren (Seite evtl. schon zerstört)
            }
        }

        private void FilterNavigationView_SelectionChanged(
           NavigationView sender,
           NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is NavigationViewItem item)
            {
                switch (item.Tag?.ToString())
                {
                    case "date":
                        ContentFrame.Navigate(typeof(DateFilterPage));
                        break;

                    case "size":
                        ContentFrame.Navigate(typeof(SizeFilterPage));
                        break;

                    case "attributes":
                        ContentFrame.Navigate(typeof(AttributeFilterPage));
                        break;

                    case "content":
                        ContentFrame.Navigate(typeof(FileContentFilterPage));
                        break;
                }
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(DateFilterPage))
            {
                dateFilterPage = e.Content as DateFilterPage;
            }
            else if (e.SourcePageType == typeof(AttributeFilterPage))
            {
                attributeFilterPage = e.Content as AttributeFilterPage;
            }
            else if (e.SourcePageType == typeof(FileContentFilterPage))
            {
                fileContentFilterPage = e.Content as FileContentFilterPage;
            }
            else if (e.SourcePageType == typeof(SizeFilterPage))
            {
                sizeFilterPage = e.Content as SizeFilterPage;
            }
        }

        private static FileAttribute GetSelectedFileAttributes(AttributeFilterPage attributeFilterPage)
        {
            FileAttribute selectedAttributes = FileAttribute.None;

            if (attributeFilterPage.IsReadonlyChecked)
            {
                selectedAttributes |= FileAttribute.Readonly;
            }

            if (attributeFilterPage.IsHiddenChecked)
            {
                selectedAttributes |= FileAttribute.Hidden;
            }

            if (attributeFilterPage.IsSystemChecked)
            {
                selectedAttributes |= FileAttribute.System;
            }

            if (attributeFilterPage.IsArchiveChecked)
            {
                selectedAttributes |= FileAttribute.Archive;
            }

            if (attributeFilterPage.IsTempChecked)
            {
                selectedAttributes |= FileAttribute.Temporary;
            }

            if (attributeFilterPage.IsCompressedChecked)
            {
                selectedAttributes |= FileAttribute.Compress;
            }

            if (attributeFilterPage.IsEncryptedChecked)
            {
                selectedAttributes |= FileAttribute.Encrypted;
            }

            return selectedAttributes;
        }


        private async void SearchButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (currentSearchEngine == null)
            {
                return;
            }


            //Setup filters
            filters.Clear();
            switch (currentSearchEngine.Kind)
            {
                case SearchEngine.EngineType.Native:
                    if (SearchPatternTextBox.Text != "")
                    {
                        filters.Add(new FileNameFilter(SearchPatternTextBox.Text));
                    }
                    if (sizeFilterPage?.SizeFilterEnabled == true)
                    {
                        filters.Add(new FileSizeFilter(sizeFilterPage.FileSizeKib, sizeFilterPage.CompareMode));
                    }
                    if (dateFilterPage?.DateFilterEnabled == true)
                    {
                        filters.Add(new DateFilter(
                            dateFilterPage.startTime,
                            dateFilterPage.endTime,
                            dateFilterPage.CreationTimeEnabled,
                            dateFilterPage.ModifiedTimeEnabled,
                            dateFilterPage.LastAccessTimeEnabled));
                    }
                    if (attributeFilterPage?.FilterByFileAttributesEnabled == true)
                    {
                        filters.Add(new AttributeFilter(GetSelectedFileAttributes(attributeFilterPage)));
                    }
                    break;

                case SearchEngine.EngineType.IndexBased:
                    if (SearchPatternTextBox.Text != "")
                    {
                        filters.Add(new IndexBasedFileNameFilter(SearchPatternTextBox.Text));
                    }
                    if (sizeFilterPage?.SizeFilterEnabled == true)
                    {
                        filters.Add(new IndexBasedFileSizeFilter(sizeFilterPage.FileSizeKib, sizeFilterPage.CompareMode));
                    }
                    if (dateFilterPage?.DateFilterEnabled == true)
                    {
                        filters.Add(new IndexBasedDateFilter(
                            dateFilterPage.startTime,
                            dateFilterPage.endTime,
                            dateFilterPage.CreationTimeEnabled,
                            dateFilterPage.ModifiedTimeEnabled,
                            dateFilterPage.LastAccessTimeEnabled));
                    }
                    if (attributeFilterPage?.FilterByFileAttributesEnabled == true)
                    {
                        filters.Add(new IndexBasedAttributeFilter(GetSelectedFileAttributes(attributeFilterPage)));
                    }
                    break;
            }
            if (fileContentFilterPage?.ContentFilterEnabled == true)
            {
                filters.Add(new FileContentFilter(fileContentFilterPage.ContentSearchText));
            }


            SearchInfoTextBlock.Text = "Searching...";
            SearchButton.IsEnabled = false;

            SearchJob job = new SearchJob(currentSearchEngine, new List<DirectoryInfo>(rootSearchDirectories), new List<SearchFilter>(filters))
            {
                Priority = JobPriority.Normal,
                BlocksUI = true
            };

            await JobScheduler.Instance.ScheduleAndWaitAsync(job);

            SearchResultListView.ItemsSource = new ObservableCollection<SearchResultItem>(job.Results);
            SearchButton.IsEnabled = true;
            SearchInfoTextBlock.Text = "Finished search. Found " + job.Results.Count + " items";
        }


        private void LocationControl_SelectedDirectoryChanged(object sender, TextChangedEventArgs e)
        {
            rootSearchDirectories.Clear();

            foreach (DirectoryInfo dirInfo in LocationControl.GetDirectoryInfos())
            {
                if (dirInfo.Exists)
                {
                    rootSearchDirectories.Add(dirInfo);
                }
            }

            bool searchButtonEnabled = rootSearchDirectories.Count > 0;
            ToolTip toolTip = new ToolTip();
            if (searchButtonEnabled)
            {
                toolTip.Content = rootSearchDirectories.Count == 1
                    ? "Click here to start search"
                    : $"Click here to search in {rootSearchDirectories.Count} directories";
            }
            else
            {
                toolTip.Content = "Please enter a valid directory path";
            }
            ToolTipService.SetToolTip(SearchButton, toolTip);
            SearchButton.IsEnabled = searchButtonEnabled;
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
