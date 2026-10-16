using System.Runtime.InteropServices;
using System.Windows.Interop;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;

namespace ClipVault.Infrastructure.Windows;

public sealed partial class HotkeyService : IHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x434C4950; // "CLIP"

    private readonly HwndSourceHook _windowHook;
    private HwndSource? _source;
    private bool _isRegistered;

    public HotkeyService()
    {
        _windowHook = WindowProcedure;
    }

    public event EventHandler? HotkeyPressed;

    public bool Register(nint windowHandle, GlobalHotkey hotkey)
    {
        Unregister();

        _source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException(
                "No se pudo obtener el HwndSource de la ventana.");

        _source.AddHook(_windowHook);

        // El sistema entrega WM_HOTKEY a la ventana si el registro es exitoso.
        if (!RegisterHotKey(
                windowHandle,
                HotkeyId,
                (uint)hotkey.Modifiers,
                hotkey.VirtualKey))
        {
            _source.RemoveHook(_windowHook);
            _source = null;
            return false;
        }

        _isRegistered = true;
        return true;
    }

    public void Unregister()
    {
        if (_source is null)
        {
            return;
        }

        if (_isRegistered)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
        }

        _source.RemoveHook(_windowHook);
        _source = null;
        _isRegistered = false;
    }

    private nint WindowProcedure(
        nint hwnd,
        int message,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt64() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        return nint.Zero;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(
        nint hwnd,
        int id,
        uint modifiers,
        uint virtualKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint hwnd, int id);
}
