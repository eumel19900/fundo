using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace fundo.tool;

internal static class WindowPlacementHelper
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    internal static void PlaceWindow(
        Window window, AppWindow appWindow, double heightRatio, double widthToHeightRatio)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        double scaleFactor = GetDpiForWindow(hwnd) / 96.0;

        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        int windowHeight = (int)(workArea.Height * heightRatio);
        int windowWidth = (int)(windowHeight * widthToHeightRatio);

        windowWidth = Math.Min(windowWidth, workArea.Width);
        windowHeight = Math.Min(windowHeight, workArea.Height);

        int x = workArea.X + (workArea.Width - windowWidth) / 2;
        int y = workArea.Y + (workArea.Height - windowHeight) / 2;

        appWindow.MoveAndResize(new RectInt32(x, y, windowWidth, windowHeight));
    }
}
