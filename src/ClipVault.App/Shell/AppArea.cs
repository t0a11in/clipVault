namespace ClipVault.App.Shell;

/// <summary>Áreas principales de la aplicación.</summary>
public enum AppArea
{
    History,
    Library
}

/// <summary>
/// Opción del selector de áreas. <see cref="Glyph"/> es un carácter de la
/// fuente de iconos (Segoe Fluent Icons / Segoe MDL2 Assets).
/// </summary>
public sealed record AreaOption(AppArea Area, string Label, string Glyph, string ToolTip);
