using fundo.core;
using fundo.core.Persistence;
using fundo.core.Persistence.Filter;
using fundo.core.Search;
using fundo.core.Search.Filter;
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

namespace fundo.gui.page
{
    public sealed partial class SearchPage : Page
    {
        private SearchEngine currentSearchEngine = null;
        private CancellationTokenSource _cts = new();
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
                    break;
            }


            await StartSearchAsync();
        }

        public async Task StartSearchAsync()
        {
            SearchInfoTextBlock.Text = "Searching...";
            SearchButton.IsEnabled = false;
            currentSearchEngine.reset();

            ObservableCollection<SearchResultItem> searchResults = new ObservableCollection<SearchResultItem>();
            SearchResultListView.ItemsSource = searchResults;

            foreach (DirectoryInfo rootDir in rootSearchDirectories)
            {
                await foreach (SearchResultItem result in currentSearchEngine.SearchAsync(rootDir, _cts.Token, filters))
                {
                    searchResults.Add(result);
                    if (currentSearchEngine is NativeSearchEngine)
                    {
                        SearchInfoTextBlock.Text = "Searching... (looked already in " + (currentSearchEngine as NativeSearchEngine).DirectoriesSearched + " directories)";
                    }
                }
            }

            SearchButton.IsEnabled = true;
            SearchInfoTextBlock.Text = "Finished search. Found " + searchResults.Count + " items";
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

        private void SearchResultListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (SearchResultListView.SelectedItem is SearchResultItem)
            {
                SearchResultItem selectedItem = (SearchResultItem)SearchResultListView.SelectedItem;
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = selectedItem.FileInfo.FullName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., file not found, no associated application)
                    ToolTip toolTip = new ToolTip { Content = $"Unable to open file: {ex.Message}" };
                    ToolTipService.SetToolTip(SearchResultListView, toolTip);
                }
            }
        }
    }
}
