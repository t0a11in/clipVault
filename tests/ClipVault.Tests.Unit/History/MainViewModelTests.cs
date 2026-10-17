using ClipVault.App.Services;
using ClipVault.App.ViewModels;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;
using Moq;

namespace ClipVault.Tests.Unit.History;

/// <summary>
/// Pruebas del Historial existente (<c>MainViewModel</c>). Objetivo: proteger lo que ya
/// funciona antes de seguir evolucionando la app. No se modificó MainViewModel para
/// escribirlas: se prueba tal como está, simulando sus dependencias con Moq.
///
/// Nota: el bloqueo de captura se decide en MainWindow (code-behind) antes de llamar a
/// CaptureAsync, así que no se puede probar aquí; aquí solo se prueba el interruptor.
/// </summary>
public sealed class MainViewModelTests
{
    private readonly Mock<IClipRepository> _repository = new();
    private readonly Mock<ISaveClipDialogService> _saveDialog = new();
    private readonly Mock<ICategoryDialogService> _categoryDialog = new();
    private readonly Mock<IApplicationWindowService> _windowService = new();

    // Contenido que devolverá la "base de datos" simulada al recargar.
    private List<ClipItem> _stored = [];

    public MainViewModelTests()
    {
        // Cualquier consulta devuelve lo que haya en _stored en ese momento.
        _repository
            .Setup(r => r.GetClipsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _stored);

        // AddTemporaryAsync devuelve el mismo clip que recibe (como hace el repositorio real).
        _repository
            .Setup(r => r.AddTemporaryAsync(It.IsAny<ClipItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClipItem clip, CancellationToken _) => clip);
    }

    private MainViewModel CreateSut() =>
        new(_repository.Object, _saveDialog.Object, _categoryDialog.Object, _windowService.Object);

    private static ClipItem Clip(long id, bool permanent, string text = "texto") => new()
    {
        Id = id,
        Type = ClipType.Text,
        Content = text,
        IsPermanent = permanent,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task InitializeAsync_SplitsRecentAndSavedClips()
    {
        _stored = [Clip(1, permanent: false), Clip(2, permanent: true), Clip(3, permanent: false)];
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal(2, sut.RecentClips.Count);
        Assert.Single(sut.SavedClips);
        Assert.Equal("2 reciente(s), 1 guardado(s).", sut.Status);
    }

    [Fact]
    public async Task CaptureAsync_Text_StoresTemporaryClipAndShowsIt()
    {
        var sut = CreateSut();

        await sut.CaptureAsync(new ClipboardContent(ClipType.Text, "hola", null));

        // Se guardó un clip de texto con el contenido capturado…
        _repository.Verify(r => r.AddTemporaryAsync(
            It.Is<ClipItem>(c => c.Type == ClipType.Text && c.Content == "hola"),
            It.IsAny<CancellationToken>()), Times.Once);
        // …y aparece en Recientes.
        Assert.Equal("hola", Assert.Single(sut.RecentClips).DisplayText);
        Assert.Equal("Texto capturado.", sut.Status);
    }

    [Fact]
    public async Task CaptureAsync_Image_ShowsImageStatus()
    {
        var sut = CreateSut();

        await sut.CaptureAsync(new ClipboardContent(ClipType.Image, null, [0x89, 0x50, 0x4E, 0x47]));

        Assert.Equal("Imagen PNG capturada.", sut.Status);
    }

    [Fact]
    public void ToggleCaptureLock_TogglesStateAndStatus()
    {
        var sut = CreateSut();

        sut.ToggleCaptureLockCommand.Execute(null);
        Assert.True(sut.IsCaptureBlocked);
        Assert.Equal("Captura del portapapeles bloqueada.", sut.Status);

        sut.ToggleCaptureLockCommand.Execute(null);
        Assert.False(sut.IsCaptureBlocked);
    }

    [Fact]
    public async Task SavePermanent_DialogCancelled_DoesNotSave()
    {
        _stored = [Clip(1, permanent: false)];
        _saveDialog
            .Setup(d => d.ShowAsync(It.IsAny<ClipItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaveClipRequest?)null);
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.SelectedClip = sut.RecentClips[0];

        await sut.SavePermanentCommand.ExecuteAsync(null);

        _repository.Verify(r => r.SavePermanentlyAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SavePermanent_DialogAccepted_SavesWithTitleAndCategory()
    {
        _stored = [Clip(7, permanent: false)];
        _saveDialog
            .Setup(d => d.ShowAsync(It.IsAny<ClipItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveClipRequest("Mi título", "Trabajo"));
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.SelectedClip = sut.RecentClips[0];

        await sut.SavePermanentCommand.ExecuteAsync(null);

        _repository.Verify(r => r.SavePermanentlyAsync(7, "Mi título", "Trabajo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSelected_DeletesOnlyTheSelectedClip()
    {
        _stored = [Clip(1, permanent: false), Clip(2, permanent: false)];
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.SelectedClip = sut.RecentClips.Single(c => c.Id == 2);

        await sut.DeleteSelectedCommand.ExecuteAsync(null);

        _repository.Verify(r => r.DeleteAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAllOrMarked_WithMarks_DeletesOnlyMarkedClips()
    {
        _stored = [Clip(1, permanent: false), Clip(2, permanent: false), Clip(3, permanent: false)];
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.RecentClips.Single(c => c.Id == 3).IsMarkedForDeletion = true;

        await sut.DeleteAllOrMarkedCommand.ExecuteAsync(null);

        _repository.Verify(r => r.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteAsync(It.Is<long>(id => id != 3), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAllOrMarked_OnRecentTab_NeverDeletesSavedClips()
    {
        // Sin marcas se eliminan todos los de la pestaña abierta (Recientes),
        // pero nunca los guardados permanentemente.
        _stored = [Clip(1, permanent: false), Clip(2, permanent: true)];
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.SelectedTabIndex = 0;

        await sut.DeleteAllOrMarkedCommand.ExecuteAsync(null);

        _repository.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteAsync(2, It.IsAny<CancellationToken>()), Times.Never);
    }
}
