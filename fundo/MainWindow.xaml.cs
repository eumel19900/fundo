using fundo.gui.Job;
using fundo.gui.page;
using fundo.tool;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Runtime.InteropServices;
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
        private SearchPage searchPage;
        private SettingsPage settingsPage;
        private AboutPage aboutPage;
        private ManualPage manualPage;
        private HotkeyHelper? _hotkeyHelper;

        private const int SwMinimize = 6;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// When set to <c>true</c> before the window content is loaded, the window
        /// will be minimized automatically after placement. The existing
        /// <see cref="NotifyIconService"/> will then hide it to the notification area.
        /// </summary>
        public bool StartMinimized { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            _hotkeyHelper = new HotkeyHelper(
                WindowNative.GetWindowHandle(this),
                () => DispatcherQueue.TryEnqueue(this.Activate));
            this.Closed += MainWindow_Closed;

            // Defer sizing until the window content is loaded so UI elements (like NavigationView) are available
            if (Content is FrameworkElement root)
            {
                root.Loaded += MainWindow_Loaded;
            }
        }


        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (Content is FrameworkElement root)
                {
                    // Initialize JobScheduler with XamlRoot for dialogs
                    JobScheduler.Instance.Initialize(root.XamlRoot);
                }

                WindowPlacementHelper.PlaceWindow(
                    this, AppWindow, 0.95, 3.5 / 4.0);

                if (StartMinimized)
                {
                    ShowWindow(WindowNative.GetWindowHandle(this), SwMinimize);
                }
            }
            finally
            {
                if (Content is FrameworkElement root)
                {
                    root.Loaded -= MainWindow_Loaded;
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
                    case "search":
                        MainWindowContentFrame.Navigate(typeof(SearchPage));
                        break;

                    case "about":
                        MainWindowContentFrame.Navigate(typeof(AboutPage));
                        break;

                    case "manual":
                        MainWindowContentFrame.Navigate(typeof(ManualPage));
                        break;

                    case "Settings":
                        MainWindowContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(SettingsPage))
            {
                settingsPage = e.Content as SettingsPage;
            }
            else if (e.SourcePageType == typeof(SearchPage))
            {
                searchPage = e.Content as SearchPage;
            }
            else if (e.SourcePageType == typeof(AboutPage))
            {
                aboutPage = e.Content as AboutPage;
            }
            else if (e.SourcePageType == typeof(ManualPage))
            {
                manualPage = e.Content as ManualPage;
            }
        }

        public void UpdateGlobalHotkey() => _hotkeyHelper?.Update();

        internal void ConnectToIndexUpdateService(ScheduledIndexUpdateService service)
        {
            service.IndexingStateChanged += UpdateSearchPageAccess;
            UpdateSearchPageAccess();
        }

        private void UpdateSearchPageAccess()
        {
            bool isIndexing = App.IndexUpdateService?.IsIndexing ?? false;
            SearchPageNav.IsEnabled = !isIndexing;

            if (isIndexing && MainWindowContentFrame.CurrentSourcePageType == typeof(SearchPage))
            {
                FilterNavigationView.SelectedItem = FilterNavigationView.SettingsItem;
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args) => _hotkeyHelper?.Dispose();
    }
}
