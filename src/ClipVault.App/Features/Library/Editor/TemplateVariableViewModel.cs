using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipVault.App.Features.Library.Editor;

/// <summary>Variable de plantilla editable ($archivo, $texto, $ruta, $nombre o $fecha) con su valor actual.</summary>
public sealed partial class TemplateVariableViewModel : ObservableObject
{
    public TemplateVariableViewModel(string name, string value)
    {
        Name = name;
        this.value = value;
    }

    public string Name { get; }

    public string Token => "$" + Name;

    /// <summary>Las variables de ruta ofrecen los selectores de archivo y carpeta.</summary>
    public bool IsPath => Name is "archivo" or "ruta";

    /// <summary>Ayuda breve que se muestra junto al valor.</summary>
    public string Hint => Name switch
    {
        "archivo" => "Archivo o carpeta",
        "ruta" => "Ruta de carpeta",
        "texto" => "Texto libre",
        "nombre" => "Nombre (proyecto, rama, usuario…)",
        "fecha" => "Fecha (formato aaaa-mm-dd)",
        _ => string.Empty
    };

    [ObservableProperty]
    private string value;
}
