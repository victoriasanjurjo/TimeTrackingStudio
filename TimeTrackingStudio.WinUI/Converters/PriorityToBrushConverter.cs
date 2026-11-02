using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace TimeTrackingStudio.WinUI.Converters;

/// <summary>
/// Convierte la prioridad de un trabajo en un color temático legible.
/// </summary>
public class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var priority = value as string ?? "";
        Color color = priority switch
        {
            "Urgente" => Color.FromArgb(255, 209, 52, 56),      // Rojo
            "Alta" => Color.FromArgb(255, 234, 116, 0),         // Naranja intenso
            "Media" => Color.FromArgb(255, 0, 120, 212),        // Azul
            "Baja" => Color.FromArgb(255, 107, 107, 107),      // Gris
            _ => Colors.Gray
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
