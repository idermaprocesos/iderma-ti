using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MesaAyudaTI.Services;

namespace MesaAyudaTI.Pages;

public sealed partial class SettingsPage : Page
{
    private TicketStore Store => TicketStore.Instance;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        SessionBox.Text = Store.CurrentTechnician;
        PathBox.Text = Store.DataFilePath;
    }

    private void SaveSession_Click(object sender, RoutedEventArgs e)
    {
        var name = SessionBox.Text.Trim();
        if (name.Length < 2)
        {
            Show(InfoBarSeverity.Warning, "Nombre corto", "Escribe el nombre con el que vas a firmar las notas.");
            return;
        }

        Store.SetCurrentTechnician(name);
        SessionBox.Text = Store.CurrentTechnician;
        Show(InfoBarSeverity.Success, "Sesión guardada", $"Las próximas notas quedarán a nombre de {Store.CurrentTechnician}.");
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Restablecer datos",
            Content = "Se reemplazan los tickets y el equipo por el conjunto de ejemplo. Esta acción no se puede deshacer.",
            PrimaryButtonText = "Restablecer",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        Store.ResetToSample();
        SessionBox.Text = Store.CurrentTechnician;
        Show(InfoBarSeverity.Success, "Datos de ejemplo", "La cola volvió al conjunto inicial del equipo de TI.");
    }

    private void Show(InfoBarSeverity severity, string title, string message)
    {
        PageInfo.Severity = severity;
        PageInfo.Title = title;
        PageInfo.Message = message;
        PageInfo.IsOpen = true;
    }
}
