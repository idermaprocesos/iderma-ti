using System.Globalization;
using Microsoft.UI.Xaml;
using MesaAyudaTI.Services;

namespace MesaAyudaTI;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        var culture = new CultureInfo("es-ES");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        InitializeComponent();
        _ = TicketStore.Instance;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
