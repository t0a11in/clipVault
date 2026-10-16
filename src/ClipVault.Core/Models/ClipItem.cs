using System.ComponentModel.DataAnnotations.Schema;

namespace ClipVault.Core.Models;

public sealed class ClipItem
{
    public long Id { get; set; }

    public ClipType Type { get; set; }

    public string? Content { get; set; }

    public byte[]? ImageData { get; set; }

    public string? Title { get; set; }

    public string? Category { get; set; }

    public bool IsPermanent { get; set; }

    public DateTime CreatedAt { get; set; }

    [NotMapped]
    public string DisplayText =>
        Type == ClipType.Text
            ? Content ?? string.Empty
            : $"Imagen PNG ({ImageData?.Length ?? 0:N0} bytes)";
}
