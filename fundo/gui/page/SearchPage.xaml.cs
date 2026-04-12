using fundo.core;
using fundo.core.Persistence;
using fundo.core.Persistence.Filter;
using fundo.core.Search;
using fundo.core.Search.Filter;
using fundo.gui.Job;
using fundo.gui.Job.Jobs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fundo.gui.page
{
    public sealed partial class SearchPage : Page
    {
        private List<DirectoryInfo> rootSearchDirectories = new();

        // Filter pages
        private DateFilterPage? dateFilterPage;
        private AttributeFilterPage? attributeFilterPage;
        private FileContentFilterPage? fileContentFilterPage;
        private SizeFilterPage? sizeFilterPage;
        private Boolean needsSearchEngineInfoBarUpdate = true;

        public SearchPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            LocationControl_SelectedDirectoryChanged(null, null);
            ContentFrame.Navigated += ContentFrame_Navigated;
            UpdateSearchEngineInfoBar();
        }

        private void UpdateSearchEngineInfoBar()
        {
            if (Settings.UseIndex)
            {
                SearchEngineInfoBar.Message =
                    "Fundo prefers index-based search where available and falls back to native search for non-indexed directories. This can be changed in the settings.";
            }
            else
            {
                SearchEngineInfoBar.Message =
                    "Fundo will search directly in your filesystem. This can be slow. You can change to index based file search in the settings.";
            }

            SearchEngineInfoBar.Title = "Search engine";

            if (needsSearchEngineInfoBarUpdate)
            {
                SearchEngineInfoBar.IsOpen = true;
                needsSearchEngineInfoBarUpdate = false;
            }

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
            UpdateSearchEngineInfoBar();

            FileContentFilter? fileContentFilter = null;

            if (fileContentFilterPage?.ContentFilterEnabled == true)
            {
                fileContentFilter = new FileContentFilter(
                    fileContentFilterPage.ContentSearchText,
                    fileContentFilterPage.CaseSensitive,
                    fileContentFilterPage.UseRegex,
                    fileContentFilterPage.WholeWord,
                    fileContentFilterPage.InvertMatch);
            }

            SearchInfoTextBlock.Text = "Searching...";
            SearchButton.IsEnabled = false;

            List<DetachedFileInfo> allResults = new();

            foreach (DirectoryInfo rootDir in rootSearchDirectories)
            {
                ISearchEngine searchEngine = ChooseSearchEngineForDirectory(rootDir);
                List<ISearchFilter> filters = BuildFilters(searchEngine.Kind);

                SearchJob job = new SearchJob(searchEngine, [rootDir], filters)
                {
                    Priority = JobPriority.Normal,
                    BlocksUI = true
                };

                await JobScheduler.Instance.ScheduleAndWaitAsync(job);
                allResults.AddRange(job.Results);
            }

            IReadOnlyList<DetachedFileInfo> searchResults = allResults;

            if (fileContentFilter != null)
            {
                FullTextSearchJob fullTextSearchJob = new(searchResults, fileContentFilter)
                {
                    Priority = JobPriority.Normal,
                    BlocksUI = true
                };

                SearchInfoTextBlock.Text = "Filtering results by file content...";
                await JobScheduler.Instance.ScheduleAndWaitAsync(fullTextSearchJob);
                searchResults = fullTextSearchJob.Results;
            }

            SearchResultList.SetItems(searchResults);
            SearchButton.IsEnabled = true;
            SearchInfoTextBlock.Text = "Finished search. Found " + searchResults.Count + " items";
        }

        private static ISearchEngine ChooseSearchEngineForDirectory(DirectoryInfo directory)
        {
            if (Settings.UseIndex && SearchIndexStore.IsPathCoveredByIndexedStorageDevice(directory.FullName))
            {
                return new IndexBasedSearchEngine();
            }

            return new NativeSearchEngine();
        }

        private List<ISearchFilter> BuildFilters(ISearchEngine.EngineType engineType)
        {
            List<ISearchFilter> filters = new();

            switch (engineType)
            {
                case ISearchEngine.EngineType.Native:
                    if (SearchPatternTextBox.Text != "")
                    {
                        filters.Add(new FileNameFilter(SearchPatternTextBox.Text, useRegex: RegexCheckBox.IsChecked == true));
                    }
                    if (sizeFilterPage?.SizeFilterEnabled == true)
                    {
                        filters.Add(new FileSizeFilter(sizeFilterPage.FileSizeBytes, sizeFilterPage.CompareMode));
                    }
                    if (dateFilterPage?.DateFilterEnabled == true)
                    {
                        filters.Add(new DateFilter(
                            dateFilterPage.StartTime,
                            dateFilterPage.EndTime,
                            dateFilterPage.CreationTimeEnabled,
                            dateFilterPage.ModifiedTimeEnabled,
                            dateFilterPage.LastAccessTimeEnabled));
                    }
                    if (attributeFilterPage?.FilterByFileAttributesEnabled == true)
                    {
                        filters.Add(new AttributeFilter(GetSelectedFileAttributes(attributeFilterPage)));
                    }
                    break;

                case ISearchEngine.EngineType.IndexBased:
                    if (SearchPatternTextBox.Text != "")
                    {
                        filters.Add(new IndexBasedFileNameFilter(SearchPatternTextBox.Text, useRegex: RegexCheckBox.IsChecked == true));
                    }
                    if (sizeFilterPage?.SizeFilterEnabled == true)
                    {
                        filters.Add(new IndexBasedFileSizeFilter(sizeFilterPage.FileSizeBytes, sizeFilterPage.CompareMode));
                    }
                    if (dateFilterPage?.DateFilterEnabled == true)
                    {
                        filters.Add(new IndexBasedDateFilter(
                            dateFilterPage.StartTime,
                            dateFilterPage.EndTime,
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

            return filters;
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
    }
}
