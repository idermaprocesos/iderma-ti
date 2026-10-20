using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using MesaAyudaTI.Models;

namespace MesaAyudaTI.Services;

public static class ThemeBrushes
{
    public static Brush ForPriority(TicketPriority priority) => Get(priority switch
    {
        TicketPriority.Critica => "SystemFillColorCriticalBrush",
        TicketPriority.Alta => "SystemFillColorCautionBrush",
        TicketPriority.Media => "AccentFillColorDefaultBrush",
        _ => "SystemFillColorSuccessBrush"
    });

    public static Brush ForStatus(TicketStatus status) => Get(status switch
    {
        TicketStatus.Nuevo => "AccentFillColorDefaultBrush",
        TicketStatus.EnProgreso => "SystemFillColorAttentionBrush",
        TicketStatus.EnEspera => "SystemFillColorCautionBrush",
        TicketStatus.Resuelto => "SystemFillColorSuccessBrush",
        _ => "TextFillColorSecondaryBrush"
    });

    private static Brush Get(string key)
    {
        if (Application.Current?.Resources is not null &&
            Application.Current.Resources.ContainsKey(key) &&
            Application.Current.Resources[key] is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}
