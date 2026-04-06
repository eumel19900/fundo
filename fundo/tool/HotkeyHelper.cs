using System;
using System.Runtime.InteropServices;
using fundo.core;
using Windows.System;

namespace fundo.tool;

internal sealed class HotkeyHelper : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int GwlWndProc = -4;
    private const int HotkeyId = 9000;
    private const int SwRestore = 9;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly IntPtr _windowHandle;
    private readonly Action _hotkeyActivatedAction;
    private IntPtr _previousWindowProc;
    private WndProcDelegate? _windowProcDelegate;
    private bool _hotkeyRegistered;
    private bool _disposed;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    internal HotkeyHelper(IntPtr windowHandle, Action hotkeyActivatedAction)
    {
        _windowHandle = windowHandle;
        _hotkeyActivatedAction = hotkeyActivatedAction;

        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        _windowProcDelegate = WindowProc;
        IntPtr newProc = Marshal.GetFunctionPointerForDelegate(_windowProcDelegate);
        _previousWindowProc = SetWindowLongPtr(_windowHandle, GwlWndProc, newProc);

        RegisterConfiguredHotkey();
    }

    internal void Update()
    {
        Unregister();
        RegisterConfiguredHotkey();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Unregister();
    }

    private void RegisterConfiguredHotkey()
    {
        if (!Settings.GlobalHotkeyEnabled)
        {
            return;
        }

        string hotkeyString = Settings.GlobalHotkeyKeys;
        if (TryParseHotkey(hotkeyString, out uint modifiers, out uint virtualKeyCode))
        {
            _hotkeyRegistered = RegisterHotKey(_windowHandle, HotkeyId, modifiers, virtualKeyCode);
        }
    }

    private void Unregister()
    {
        if (_hotkeyRegistered)
        {
            UnregisterHotKey(_windowHandle, HotkeyId);
            _hotkeyRegistered = false;
        }
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            ShowWindow(_windowHandle, SwRestore);
            SetForegroundWindow(_windowHandle);
            _hotkeyActivatedAction();
            return IntPtr.Zero;
        }

        return CallWindowProc(_previousWindowProc, hWnd, msg, wParam, lParam);
    }

    internal static bool TryParseHotkey(string hotkeyString, out uint modifiers, out uint virtualKeyCode)
    {
        modifiers = 0;
        virtualKeyCode = 0;

        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return false;
        }

        string[] parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;
                case "alt":
                    modifiers |= ModAlt;
                    break;
                case "shift":
                    modifiers |= ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModWin;
                    break;
                default:
                    return false;
            }
        }

        virtualKeyCode = MapKeyNameToVirtualKey(parts[^1]);
        return virtualKeyCode != 0;
    }

    internal static string MapVirtualKeyToName(VirtualKey key)
    {
        if (key >= VirtualKey.F1 && key <= VirtualKey.F24)
        {
            return $"F{(int)key - (int)VirtualKey.F1 + 1}";
        }

        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return key.ToString();
        }

        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
        {
            return ((int)key - (int)VirtualKey.Number0).ToString();
        }

        return key switch
        {
            VirtualKey.Space => "Space",
            VirtualKey.Enter => "Enter",
            VirtualKey.Tab => "Tab",
            VirtualKey.Escape => "Escape",
            VirtualKey.Back => "Backspace",
            VirtualKey.Delete => "Delete",
            VirtualKey.Insert => "Insert",
            VirtualKey.Home => "Home",
            VirtualKey.End => "End",
            VirtualKey.PageUp => "PageUp",
            VirtualKey.PageDown => "PageDown",
            VirtualKey.Up => "Up",
            VirtualKey.Down => "Down",
            VirtualKey.Left => "Left",
            VirtualKey.Right => "Right",
            _ => ""
        };
    }

    private static uint MapKeyNameToVirtualKey(string keyName)
    {
        if (keyName.Length > 1 && keyName.StartsWith('F') &&
            int.TryParse(keyName.AsSpan(1), out int fNumber) && fNumber >= 1 && fNumber <= 24)
        {
            return (uint)(0x70 + fNumber - 1);
        }

        if (keyName.Length == 1)
        {
            char c = char.ToUpperInvariant(keyName[0]);
            if (c is >= 'A' and <= 'Z')
            {
                return c;
            }
            if (c is >= '0' and <= '9')
            {
                return c;
            }
        }

        return keyName.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "enter" => 0x0D,
            "tab" => 0x09,
            "escape" or "esc" => 0x1B,
            "backspace" => 0x08,
            "delete" or "del" => 0x2E,
            "insert" or "ins" => 0x2D,
            "home" => 0x24,
            "end" => 0x23,
            "pageup" => 0x21,
            "pagedown" => 0x22,
            "up" => 0x26,
            "down" => 0x28,
            "left" => 0x25,
            "right" => 0x27,
            _ => 0
        };
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newWindowProc)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, newWindowProc);
        }

        return new IntPtr(SetWindowLong32(hWnd, nIndex, newWindowProc.ToInt32()));
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
