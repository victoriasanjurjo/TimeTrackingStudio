using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace TimeTrackingStudio.WinUI.Converters;

/// <summary>
/// Convierte el estado de un trabajo en un color temático legible.
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value as string ?? "";
        Color color = status switch
        {
            "Pendiente" => Color.FromArgb(255, 180, 140, 20),      // Ámbar/Dorado
            "En progreso" => Color.FromArgb(255, 216, 59, 1),       // Naranja cálido
            "En revisión" => Color.FromArgb(255, 0, 120, 212),      // Azul acento
            "Entregado" => Color.FromArgb(255, 16, 124, 65),       // Verde bosque
            "Facturado" => Color.FromArgb(255, 135, 100, 184),    // Púrpura ejecutivo
            _ => Colors.Gray
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
