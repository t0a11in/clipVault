using ClipVault.Core.Models;

namespace ClipVault.Core.Interfaces;

public interface IHotkeyService
{
    event EventHandler? HotkeyPressed;

    bool Register(nint windowHandle, GlobalHotkey hotkey);

    void Unregister();
}
