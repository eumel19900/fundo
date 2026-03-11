using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using fundo.core;
using fundo.gui;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace fundo
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private NotifyIconService? _notifyIconService;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Static accessor to the application's main window instance
        public static MainWindow? MainWindowInstance { get; private set; }

        private const int SwRestore = 9;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Initialize application-wide session and shared services
            Session.Initialize();

            if (HasCommandLineArgument(args.Arguments, "--UpdateIndexOnly"))
            {
                return;
            }

            EnsureMainWindowIsOpen();
            InitializeNotifyIcon();
        }

        private static bool HasCommandLineArgument(string launchArguments, string argument)
        {
            if (!string.IsNullOrWhiteSpace(launchArguments))
            {
                string[] arguments = launchArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (arguments.Contains(argument, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return Environment.GetCommandLineArgs()
                .Contains(argument, StringComparer.OrdinalIgnoreCase);
        }

        private void InitializeNotifyIcon()
        {
            if (MainWindowInstance == null)
            {
                return;
            }

            _notifyIconService ??= new NotifyIconService();
            _notifyIconService.Initialize(MainWindowInstance, OpenMainWindowFromNotifyIcon, CloseApplicationFromNotifyIcon);
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            _notifyIconService?.Dispose();
        }

        private void EnsureMainWindowIsOpen()
        {
            if (MainWindowInstance != null)
            {
                MainWindowInstance.Activate();
                return;
            }

            _window = new MainWindow();
            MainWindowInstance = _window as MainWindow;
            if (MainWindowInstance != null)
            {
                MainWindowInstance.Closed += (_, _) => _notifyIconService?.Dispose();
            }
            _window.Activate();
        }

        private static void BringMainWindowToFront()
        {
            if (MainWindowInstance == null)
            {
                return;
            }

            IntPtr windowHandle = WindowNative.GetWindowHandle(MainWindowInstance);
            ShowWindow(windowHandle, SwRestore);
            SetForegroundWindow(windowHandle);
        }

        private static void OpenMainWindowFromNotifyIcon()
        {
            if (Application.Current is not App app)
            {
                return;
            }

            app.EnsureMainWindowIsOpen();
            BringMainWindowToFront();
        }

        private static void CloseApplicationFromNotifyIcon()
        {
            if (Application.Current is not App app)
            {
                return;
            }

            app._notifyIconService?.Dispose();
            MainWindowInstance?.Close();
            app.Exit();
        }
    }
}
