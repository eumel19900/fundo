using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace fundo.gui;

internal sealed class NotifyIconService : IDisposable
{
    private const int GwlWndProc = -4;
    private const int SwHide = 0;
    private const int WmApp = 0x8000;
    private const int WmTrayIcon = WmApp + 1;
    private const int WmCommand = 0x0111;
    private const int WmNull = 0x0000;
    private const int WmSize = 0x0005;
    private const int WmRButtonUp = 0x0205;
    private const int WmLButtonDblClk = 0x0203;
    private const int SizeMinimized = 1;
    private const int TrayIconId = 1;
    private const int OpenFundoMenuCommandId = 1001;
    private const int CloseFundoMenuCommandId = 1002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NifInfo = 0x00000010;
    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;
    private const uint NiifInfo = 0x00000001;
    private const uint ImageIcon = 1;
    private const uint LrLoadFromFile = 0x00000010;
    private const uint LrDefaultSize = 0x00000040;
    private const uint MfString = 0x00000000;
    private const uint TpmLeftAlign = 0x0000;
    private const uint TpmBottomAlign = 0x0020;
    private const uint TpmRightButton = 0x0002;

    private IntPtr _windowHandle;
    private IntPtr _notifyIconHandle;
    private IntPtr _previousWindowProc;
    private WndProcDelegate? _windowProcDelegate;
    private NOTIFYICONDATA _notifyIconData;
    private bool _isNotifyIconCreated;
    private bool _hasShownMinimizeToTrayHint;
    private Action? _openMainWindowAction;
    private Action? _closeApplicationAction;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public void Initialize(Window window, Action openMainWindowAction, Action closeApplicationAction)
    {
        if (_isNotifyIconCreated || window == null)
        {
            return;
        }

        _openMainWindowAction = openMainWindowAction;
        _closeApplicationAction = closeApplicationAction;

        _windowHandle = WindowNative.GetWindowHandle(window);
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (_previousWindowProc == IntPtr.Zero)
        {
            _windowProcDelegate = WindowProc;
            IntPtr newWindowProc = Marshal.GetFunctionPointerForDelegate(_windowProcDelegate);
            _previousWindowProc = SetWindowLongPtr(_windowHandle, GwlWndProc, newWindowProc);
        }

        string iconPath = Path.Combine(AppContext.BaseDirectory, "fundo.ico");
        if (!File.Exists(iconPath))
        {
            return;
        }

        _notifyIconHandle = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 0, 0, LrLoadFromFile | LrDefaultSize);
        if (_notifyIconHandle == IntPtr.Zero)
        {
            return;
        }

        _notifyIconData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = NifMessage | NifIcon | NifTip,
            uCallbackMessage = WmTrayIcon,
            hIcon = _notifyIconHandle,
            szTip = "Fundo"
        };

        _isNotifyIconCreated = Shell_NotifyIcon(NimAdd, ref _notifyIconData);
    }

    public void Dispose()
    {
        RemoveNotifyIcon();
        RestoreWindowProc();
        GC.SuppressFinalize(this);
    }

    private void ShowNotifyIconContextMenu()
    {
        IntPtr menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MfString, OpenFundoMenuCommandId, "Open Fundo...");
            AppendMenu(menu, MfString, CloseFundoMenuCommandId, "Close");

            if (GetCursorPos(out POINT point))
            {
                SetForegroundWindow(_windowHandle);
                TrackPopupMenuEx(menu, TpmLeftAlign | TpmBottomAlign | TpmRightButton, point.X, point.Y, _windowHandle, IntPtr.Zero);
                PostMessage(_windowHandle, WmNull, IntPtr.Zero, IntPtr.Zero);
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private void ShowMinimizeToTrayHint()
    {
        if (!_isNotifyIconCreated || _hasShownMinimizeToTrayHint)
        {
            return;
        }

        string message = "The app was minimized to the notification area and continues running there.";
        if (core.Settings.GlobalHotkeyEnabled)
        {
            message += $"\nPress {core.Settings.GlobalHotkeyKeys} to open Fundo again.";
        }

        NOTIFYICONDATA notifyIconData = _notifyIconData;
        notifyIconData.uFlags = NifInfo;
        notifyIconData.dwInfoFlags = NiifInfo;
        notifyIconData.uTimeoutOrVersion = 10000;
        notifyIconData.szInfoTitle = "Fundo is still running";
        notifyIconData.szInfo = message;

        if (Shell_NotifyIcon(NimModify, ref notifyIconData))
        {
            _hasShownMinimizeToTrayHint = true;
        }
    }

    private void HideMainWindowToTray(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(windowHandle, SwHide);
    }

    private void RemoveNotifyIcon()
    {
        if (_isNotifyIconCreated)
        {
            Shell_NotifyIcon(NimDelete, ref _notifyIconData);
            _isNotifyIconCreated = false;
        }

        if (_notifyIconHandle != IntPtr.Zero)
        {
            DestroyIcon(_notifyIconHandle);
            _notifyIconHandle = IntPtr.Zero;
        }
    }

    private void RestoreWindowProc()
    {
        if (_windowHandle == IntPtr.Zero || _previousWindowProc == IntPtr.Zero)
        {
            return;
        }

        SetWindowLongPtr(_windowHandle, GwlWndProc, _previousWindowProc);
        _previousWindowProc = IntPtr.Zero;
        _windowProcDelegate = null;
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmSize && wParam.ToInt32() == SizeMinimized)
        {
            HideMainWindowToTray(hWnd);
            ShowMinimizeToTrayHint();
            return IntPtr.Zero;
        }

        if (msg == WmTrayIcon)
        {
            int trayMessage = lParam.ToInt32();
            if (trayMessage == WmRButtonUp)
            {
                ShowNotifyIconContextMenu();
                return IntPtr.Zero;
            }

            if (trayMessage == WmLButtonDblClk)
            {
                _openMainWindowAction?.Invoke();
                return IntPtr.Zero;
            }
        }

        if (msg == WmCommand && GetLowWord(wParam) == OpenFundoMenuCommandId)
        {
            _openMainWindowAction?.Invoke();
            return IntPtr.Zero;
        }

        if (msg == WmCommand && GetLowWord(wParam) == CloseFundoMenuCommandId)
        {
            _closeApplicationAction?.Invoke();
            return IntPtr.Zero;
        }

        return CallWindowProc(_previousWindowProc, hWnd, msg, wParam, lParam);
    }

    private static int GetLowWord(IntPtr value)
    {
        return unchecked((int)((long)value & 0xFFFF));
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newWindowProc)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, newWindowProc);
        }

        return new IntPtr(SetWindowLong32(hWnd, nIndex, newWindowProc.ToInt32()));
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
