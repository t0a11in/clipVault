namespace ClipVault.Core.Models;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public readonly record struct GlobalHotkey(
    HotkeyModifiers Modifiers,
    uint VirtualKey)
{
    public static GlobalHotkey CtrlAltI =>
        new(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x49);

    public static GlobalHotkey CtrlI =>
        new(HotkeyModifiers.Control, 0x49);
}
