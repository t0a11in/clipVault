namespace ClipVault.Core.Models;

public sealed record ClipboardContent(
    ClipType Type,
    string? Text,
    byte[]? ImageData);
