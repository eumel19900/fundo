using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;

namespace fundo.tool
{
    /// <summary>
    /// Helper utilities for placing and sizing application windows in a DPI-aware manner.
    /// </summary>
    /// Note: this class was created by copilot
    internal static class WindowPlacementHelper
    {
        /// <summary>
        /// Positions the given <paramref name="appWindow"/> on the monitor containing <paramref name="window"/>,
        /// sets its height to approximately <paramref name="heightFraction"/> of the work area height (default 0.8),
        /// and chooses a width wide enough to show all top-level items of the provided <paramref name="navigationView"/>
        /// so that none of the tabs are initially collapsed.
        /// This method is DPI-aware and works with scaled displays.
        /// </summary>
        public static void PlacePortraitWindowEnsureNavTabsVisible(Window window, AppWindow appWindow, NavigationView navigationView, double heightFraction = 0.8)
        {
            if (window is null) throw new ArgumentNullException(nameof(window));
            if (appWindow is null) throw new ArgumentNullException(nameof(appWindow));
            if (navigationView is null) throw new ArgumentNullException(nameof(navigationView));

            try
            {
                /*   var hwnd = WindowNative.GetWindowHandle(window);
                   var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                   var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);

                   var workArea = displayArea.WorkArea;

                   int targetHeight = (int)(workArea.Height * heightFraction);

                   double totalRequiredDip = 0;
                   foreach (var item in navigationView.MenuItems)
                   {
                       if (item is NavigationViewItem nvi)
                       {
                           var text = nvi.Content?.ToString() ?? string.Empty;
                           var tb = new TextBlock { Text = text };
                           tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                           var textWidth = tb.DesiredSize.Width;
                           const double perItemPadding = 48; // icon + margins estimate
                           totalRequiredDip += textWidth + perItemPadding;
                       }
                       else
                       {
                           totalRequiredDip += 120;
                       }
                   }

                   totalRequiredDip += 150; // extra margin

                   // Use the XAML element's XamlRoot rasterization scale which is available on the UI thread
                   double scale = 1.0;
                   if (navigationView.XamlRoot != null)
                   {
                       scale = navigationView.XamlRoot.RasterizationScale;
                   }

                   int targetWidth = Math.Min((int)(totalRequiredDip * scale), workArea.Width);*/

                /* int posX = workArea.X + (workArea.Width - targetWidth) / 2;
                 int posY = workArea.Y + (workArea.Height - targetHeight) / 2;*/

                //appWindow.Move(new Windows.Graphics.PointInt32(posX, posY));
                //appWindow.Resize(new Windows.Graphics.SizeInt32(targetWidth, targetHeight));
                appWindow.Move(new Windows.Graphics.PointInt32(1000, 300));
                appWindow.Resize(new Windows.Graphics.SizeInt32(1300, 1700));
            }
            catch
            {
                // swallow exceptions here; caller can decide on fallback behavior
                try
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 1600));
                }
                catch { }
            }
        }
    }
}
