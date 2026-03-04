using fundo.core;
using fundo.core.Search;
using fundo.core.Search.Index;
using fundo.core.Search.Native;
using fundo.core.Search.Native.Filter;
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
        private DirectoryInfo rootSearchDirectoryInfo = null;

        // Filter pages
        private DateFilterPage dateFilterPage;
        private AttributeFilterPage attributeFilterPage;
        private FileContentFilterPage fileContentFilterPage;
        private SizeFilterPage sizeFilterPage;

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

        private async void SeachButton_Clicked(object sender, RoutedEventArgs e)
        {
            await StartSearchAsync();
        }

        public async Task StartSearchAsync()
        {
            SearchInfoTextBlock.Text = "Searching...";
            SearchButton.IsEnabled = false;
            currentSearchEngine.reset();
            List<SearchFilter> searchFilters = new();

            if (SearchPatternTextBox.Text != "")
            {
                searchFilters.Add(new FileNameFilter(SearchPatternTextBox.Text));
            }

            if (dateFilterPage.DateFilterEnabled)
            {
                searchFilters.Add(new DateFilter(dateFilterPage.startTime, dateFilterPage.endTime));
            }

            ObservableCollection<SearchResultItem> searchResults = new ObservableCollection<SearchResultItem>();
            SearchResultListView.ItemsSource = searchResults;
            await foreach (SearchResultItem result in currentSearchEngine.SearchAsync(rootSearchDirectoryInfo, _cts.Token, searchFilters))
            {
                searchResults.Add(result);
                if (currentSearchEngine is NativeSearchEngine)
                {
                    SearchInfoTextBlock.Text = "Searching... (looked already in " + (currentSearchEngine as NativeSearchEngine).DirectoriesSearched + " directories)";
                }
            }
            SearchButton.IsEnabled = true;
            SearchInfoTextBlock.Text = "Finished search. Found " + searchResults.Count + " items";
        }


        private void LocationControl_SelectedDirectoryChanged(object sender, TextChangedEventArgs e)
        {
            rootSearchDirectoryInfo = null;
            try
            {
                rootSearchDirectoryInfo = new DirectoryInfo(LocationControl.SelectedDirectory);
            }
            catch { }
            bool searchButtonEnabled = rootSearchDirectoryInfo != null && rootSearchDirectoryInfo.Exists;
            ToolTip toolTip = new ToolTip();
            if (searchButtonEnabled)
            {
                toolTip.Content = "Click here to start search";
            }
            else
            {
                //this does not work because the SearchButton is disabled, but it shows the idea of providing user feedback on why the button is disabled (comment by copilot :-))
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
