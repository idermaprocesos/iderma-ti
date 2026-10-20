using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MesaAyudaTI.Models;
using MesaAyudaTI.Services;

namespace MesaAyudaTI.Pages;

public sealed class CategoryBar
{
    public required string Name { get; init; }
    public int Count { get; init; }
    public int Maximum { get; init; }
}

public sealed partial class PanelPage : Page
{
    private TicketStore Store => TicketStore.Instance;

    public PanelPage()
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
        var firstName = Store.CurrentTechnician.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? Store.CurrentTechnician;
        GreetingText.Text = $"{TimeText.Greeting()}, {firstName}";
        DateText.Text = TimeText.Today();

        SaveErrorBar.IsOpen = Store.LastError is not null;
        SaveErrorBar.Message = Store.LastError ?? "";

        var tickets = Store.Tickets.ToList();
        var open = tickets.Where(t => t.IsOpen).ToList();
        var weekAgo = DateTimeOffset.Now.AddDays(-7);

        OpenCard.Value = open.Count(t => t.Status == TicketStatus.Nuevo).ToString();
        ProgressCard.Value = open.Count(t => t.Status is TicketStatus.EnProgreso or TicketStatus.EnEspera).ToString();
        CriticalCard.Value = open.Count(t => t.Priority == TicketPriority.Critica).ToString();
        ResolvedCard.Value = tickets.Count(t => !t.IsOpen && (t.ResolvedAt ?? t.UpdatedAt) >= weekAgo).ToString();

        var recent = tickets
            .OrderByDescending(t => t.UpdatedAt)
            .ThenByDescending(t => t.Id)
            .Take(6)
            .ToList();
        RecentList.ItemsSource = recent;
        EmptyRecentText.Visibility = recent.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var groups = open
            .GroupBy(t => t.CategoryLabel)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();
        var maximum = Math.Max(1, groups.Count == 0 ? 1 : groups.Max(g => g.Count));
        CategoryList.ItemsSource = groups
            .Select(g => new CategoryBar { Name = g.Name, Count = g.Count, Maximum = maximum })
            .ToList();
        EmptyCategoryText.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CategoryList.Visibility = groups.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void NewTicket_Click(object sender, RoutedEventArgs e) => AppNavigator.Go("nuevo");

    private void ViewAll_Click(object sender, RoutedEventArgs e) => AppNavigator.Go("tickets");

    private void RecentList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not Ticket ticket)
            return;

        Store.RequestSelection(ticket.Id);
        AppNavigator.Go("tickets");
    }
}
