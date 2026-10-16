using ClipVault.Core.Models;

namespace ClipVault.Core.Interfaces;

public interface IClipboardMonitor
{
    event EventHandler<ClipboardContentEventArgs>? ClipboardChanged;

    void Start(nint windowHandle);

    void Stop();
}

public sealed class ClipboardContentEventArgs(ClipboardContent content) : EventArgs
{
    public ClipboardContent Content { get; } = content;
}
