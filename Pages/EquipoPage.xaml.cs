using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MesaAyudaTI.Services;
using Windows.System;

namespace MesaAyudaTI.Pages;

public sealed class TechnicianRow
{
    public required string Name { get; init; }
    public int OpenTickets { get; init; }
    public bool IsCurrent { get; init; }

    public string LoadLabel => OpenTickets == 1
        ? "1 ticket abierto"
        : $"{OpenTickets} tickets abiertos";
}

public sealed partial class EquipoPage : Page
{
    private TicketStore Store => TicketStore.Instance;

    public EquipoPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Store.Changed -= OnStoreChanged;
        Store.Changed += OnStoreChanged;
        Refresh();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Store.Changed -= OnStoreChanged;
    }

    private void OnStoreChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        TeamList.ItemsSource = Store.Technicians
            .Select(name => new TechnicianRow
            {
                Name = name,
                IsCurrent = string.Equals(name, Store.CurrentTechnician, StringComparison.CurrentCultureIgnoreCase),
                OpenTickets = Store.Tickets.Count(ticket =>
                    ticket.IsOpen && string.Equals(ticket.Assignee, name, StringComparison.CurrentCultureIgnoreCase))
            })
            .ToList();
    }

    private void Add_Click(object sender, RoutedEventArgs e) => AddTechnician();

    private void NameBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        AddTechnician();
    }

    private void AddTechnician()
    {
        if (!Store.TryAddTechnician(NameBox.Text, out var error))
        {
            Show(InfoBarSeverity.Warning, "No se agregó", error ?? "Revisa el nombre.");
            return;
        }

        NameBox.Text = "";
        Show(InfoBarSeverity.Success, "Listo", "La persona ya puede recibir tickets.");
    }

    private void UseSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string name })
            return;

        Store.SetCurrentTechnician(name);
        Show(InfoBarSeverity.Success, "Sesión actualizada", $"Las notas quedarán a nombre de {name}.");
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string name })
            return;

        var blocked = Store.WhyCannotRemove(name);
        if (blocked is not null)
        {
            Show(InfoBarSeverity.Warning, "No se puede quitar", blocked);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Quitar del equipo",
            Content = $"¿Quitar a {name} del equipo de TI? Los tickets que ya cerró se conservan.",
            PrimaryButtonText = "Quitar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        if (!Store.TryRemoveTechnician(name, out var error))
        {
            Show(InfoBarSeverity.Warning, "No se puede quitar", error ?? "Inténtalo de nuevo.");
            return;
        }

        Show(InfoBarSeverity.Success, "Listo", $"{name} salió del equipo.");
    }

    private void Show(InfoBarSeverity severity, string title, string message)
    {
        PageInfo.Severity = severity;
        PageInfo.Title = title;
        PageInfo.Message = message;
        PageInfo.IsOpen = true;
    }
}
