using System.Windows;
using System.Windows.Controls;
using ClipVault.App.Features.Library.Editor;
using ClipVault.Core.Models.Library;

namespace ClipVault.App.Components.Fields;

/// <summary>
/// Elige el componente según el tipo de campo (equivalente a *ngSwitch).
/// Para agregar un tipo nuevo: crear su componente, su DataTemplate y una línea aquí.
/// Los campos sensibles siempre usan la plantilla segura, sea cual sea su tipo.
/// </summary>
public sealed class FieldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? CodeTemplate { get; set; }

    public DataTemplate? SecretTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not FieldViewModel field)
        {
            return base.SelectTemplate(item, container);
        }

        if (field.IsSensitive)
        {
            return SecretTemplate;
        }

        return field.Type switch
        {
            FieldType.Code => CodeTemplate,
            _ => TextTemplate
        };
    }
}
