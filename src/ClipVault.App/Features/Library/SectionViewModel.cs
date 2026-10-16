using ClipVault.Core.Models.Library;

namespace ClipVault.App.Features.Library;

/// <summary>
/// Sección en el panel de navegación, con renombrado en línea (ver <see cref="Rename"/>).
/// Si se cancela el nombre de una sección recién creada, se descarta.
/// </summary>
public sealed class SectionViewModel : NavItemViewModel
{
    public SectionViewModel(Section model, Action<SectionViewModel> onDiscardNew, bool isNew = false)
    {
        Model = model;
        IsNew = isNew;
        Label = model.Name;
        Glyph = LibraryIcons.FromKey(model.IconKey);
        ColorHex = model.ColorHex;

        Rename = new InlineRenameViewModel(
            currentName: () => Label,
            apply: name =>
            {
                Label = name;
                Model.Name = name;
                IsNew = false;
            },
            onCancelled: () =>
            {
                if (IsNew)
                {
                    onDiscardNew(this);
                }
            });

        // Label se declara en la clase base: el aviso de cambio del ToolTip se encadena aquí.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Label))
            {
                OnPropertyChanged(nameof(ToolTip));
            }
        };
    }

    public Section Model { get; }

    /// <summary>Recién creada y aún sin confirmar su nombre.</summary>
    public bool IsNew { get; private set; }

    public InlineRenameViewModel Rename { get; }

    public string ToolTip => $"{Label} · F2 para renombrar";

    public void BeginRename() => Rename.Begin();
}
