using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;

namespace ClipVault.Infrastructure.Windows;

public sealed partial class ClipboardMonitor : IClipboardMonitor
{
    private const int WmClipboardUpdate = 0x031D;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(1);

    private readonly HwndSourceHook _windowHook;
    private HwndSource? _source;
    private bool _isListening;
    private string? _lastCaptureSignature;
    private DateTime _lastCaptureAtUtc;

    public ClipboardMonitor()
    {
        _windowHook = WindowProcedure;
    }

    public event EventHandler<ClipboardContentEventArgs>? ClipboardChanged;

    public void Start(nint windowHandle)
    {
        if (_isListening)
        {
            return;
        }

        _source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException(
                "No se pudo obtener el HwndSource de la ventana.");

        _source.AddHook(_windowHook);

        // Registra WM_CLIPBOARDUPDATE y elimina la necesidad de hacer polling.
        if (!AddClipboardFormatListener(windowHandle))
        {
            _source.RemoveHook(_windowHook);
            _source = null;
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _isListening = true;
    }

    public void Stop()
    {
        if (!_isListening || _source is null)
        {
            return;
        }

        RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(_windowHook);
        _source = null;
        _isListening = false;
    }

    private nint WindowProcedure(
        nint hwnd,
        int message,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        if (message == WmClipboardUpdate)
        {
            TryReadClipboard();
        }

        return nint.Zero;
    }

    private void TryReadClipboard()
    {
        try
        {
            // Cuando hay varios formatos disponibles, texto Unicode es prioritario.
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                var text = Clipboard.GetText(TextDataFormat.UnicodeText);

                PublishIfNew(new ClipboardContent(ClipType.Text, text, null));

                return;
            }

            if (!Clipboard.ContainsImage())
            {
                return;
            }

            var bitmap = Clipboard.GetImage();

            if (bitmap is null)
            {
                return;
            }

            PublishIfNew(new ClipboardContent(
                ClipType.Image,
                null,
                ConvertToPng(bitmap)));
        }
        catch (ExternalException)
        {
            // Algunas aplicaciones bloquean brevemente el portapapeles.
        }
    }

    private static byte[] ConvertToPng(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);

        return stream.ToArray();
    }

    private void PublishIfNew(ClipboardContent content)
    {
        var signature = content.Type == ClipType.Text
            ? $"text:{content.Text}"
            : $"image:{Convert.ToHexString(SHA256.HashData(content.ImageData!))}";

        var now = DateTime.UtcNow;

        // Herramientas como Recortes pueden notificar el mismo PNG dos veces al
        // publicar varios formatos del portapapeles. Solo se omite el duplicado inmediato.
        if (signature == _lastCaptureSignature && now - _lastCaptureAtUtc < DuplicateWindow)
        {
            return;
        }

        _lastCaptureSignature = signature;
        _lastCaptureAtUtc = now;

        ClipboardChanged?.Invoke(
            this,
            new ClipboardContentEventArgs(content));
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AddClipboardFormatListener(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveClipboardFormatListener(nint hwnd);
}
