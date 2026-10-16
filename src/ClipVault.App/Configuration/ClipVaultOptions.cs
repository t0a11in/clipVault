using ClipVault.Core.Models;

namespace ClipVault.App.Configuration;

public sealed class ClipVaultOptions
{
    public bool UseCtrlI { get; init; }

    public GlobalHotkey Hotkey =>
        UseCtrlI ? GlobalHotkey.CtrlI : GlobalHotkey.CtrlAltI;

    public static ClipVaultOptions FromArguments(string[] arguments)
    {
        return new ClipVaultOptions
        {
            // Ctrl+I queda disponible como opción explícita y no es el valor seguro por defecto.
            UseCtrlI = arguments.Any(argument =>
                string.Equals(
                    argument,
                    "--shortcut=Ctrl+I",
                    StringComparison.OrdinalIgnoreCase))
        };
    }
}
