using fundo.core.Search;
using fundo.gui;
using fundo.gui.tool;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace fundo
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private SearchEngine currentSearchEngine = new NativeSearchEngine();
        private CancellationTokenSource _cts = new();
        private DirectoryInfo rootSearchDirectoryInfo = null;
        
        //Filter pages
        private DateFilterPage dateFilterPage;
        private AttributeFilterPage attributeFilterPage;
        private FileContentFilterPage fileContentFilterPage;
        private SizeFilterPage sizeFilterPage;


        public MainWindow()
        {
            InitializeComponent();
            // Defer sizing until the window content is loaded so UI elements (like NavigationView) are available
            if (Content is FrameworkElement root)
            {
                root.Loaded += MainWindow_Loaded;
            }
            LocationControl_SelectedDirectoryChanged(null, null);
            ContentFrame.Navigated += ContentFrame_Navigated;
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

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (Content is FrameworkElement)
                {
                    // Use the helper to place and size the window in a DPI-aware way
                    WindowPlacementHelper.PlacePortraitWindowEnsureNavTabsVisible(this, AppWindow, FilterNavigationView, 0.8);
                }
            }
            finally
            {
                if (Content is FrameworkElement root2)
                {
                    root2.Loaded -= MainWindow_Loaded;
                }
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

        private async void SeachButton_Clicked(object sender, RoutedEventArgs e)
        {
            await StartSearchAsync();
        }

        public async Task StartSearchAsync()
        {
            SearchResultListView.Items.Clear();

            List<SearchFilter> searchFilters = new();
            if(dateFilterPage.DateFilterEnabled)
            {
                searchFilters.Add(new DateFilter(dateFilterPage.startTime, dateFilterPage.endTime));
            }

            await foreach (SearchResult result in currentSearchEngine.SearchAsync(rootSearchDirectoryInfo, _cts.Token, searchFilters))
            {
                SearchResultListView.Items.Add(result.FileName);
            }
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
    }
}
