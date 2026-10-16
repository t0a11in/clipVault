using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipVault.App.Features.Library.Editor;

public enum NoticeTone
{
    Success,
    Warning,
    Error
}

/// <summary>
/// Aviso breve ("Copiado al portapapeles"). Se oculta solo tras unos segundos.
/// Usa Task.Delay (no un temporizador de UI) y cancela el aviso anterior si llega otro.
/// Nunca muestra contenido copiado: solo el mensaje de estado.
/// </summary>
public sealed partial class NoticeViewModel : ObservableObject
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2.5);
    private CancellationTokenSource? _hideCancellation;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private NoticeTone tone;

    [ObservableProperty]
    private bool isVisible;

    public void Show(string text, NoticeTone noticeTone = NoticeTone.Success)
    {
        _hideCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _hideCancellation = cancellation;

        Message = text;
        Tone = noticeTone;
        IsVisible = true;

        // Se descarta la tarea a propósito: el ocultamiento no debe bloquear a quien llama.
        _ = HideLaterAsync(noticeTone == NoticeTone.Success ? DefaultDuration : DefaultDuration * 2, cancellation.Token);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Dismiss()
    {
        _hideCancellation?.Cancel();
        IsVisible = false;
    }

    private async Task HideLaterAsync(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            IsVisible = false;
        }
        catch (TaskCanceledException)
        {
            // Llegó otro aviso (o se cerró a mano): no hay nada que ocultar.
        }
    }
}
