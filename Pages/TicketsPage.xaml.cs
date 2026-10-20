using System.Collections.ObjectModel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MesaAyudaTI.Models;
using MesaAyudaTI.Services;
using Windows.System;
using Windows.UI.Core;

namespace MesaAyudaTI.Pages;

public sealed partial class TicketsPage : Page
{
    private const string Unassigned = "Sin asignar";

    private readonly List<LabeledOption<TicketStatus>> _statuses = Catalog.StatusChoices();
    private readonly List<LabeledOption<TicketPriority>> _priorities = Catalog.PriorityChoices();
    private readonly TicketStore _store = TicketStore.Instance;
    private bool _filtersReady;
    private bool _syncing;
    private int? _shownId;

    public ObservableCollection<Ticket> FilteredTickets { get; } = new();

    public TicketsPage()
    {
        InitializeComponent();
        StatusFilter.ItemsSource = Catalog.StatusFilters();
        PriorityFilter.ItemsSource = Catalog.PriorityFilters();
        StatusFilter.SelectedIndex = 0;
        PriorityFilter.SelectedIndex = 0;
        StatusBox.ItemsSource = _statuses;
        PriorityBox.ItemsSource = _priorities;
        TicketsList.ItemsSource = FilteredTickets;
        _filtersReady = true;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _store.Changed -= OnStoreChanged;
        _store.Changed += OnStoreChanged;

        var pending = _store.ConsumePendingSelection();
        if (pending is int id)
        {
            ResetFilters();
            ApplyFilter(id);
            if (TicketsList.SelectedItem is not null)
                TicketsList.ScrollIntoView(TicketsList.SelectedItem);
            return;
        }

        if (FilteredTickets.Count == 0)
            ApplyFilter(null);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _store.Changed -= OnStoreChanged;
    }

    public void FocusPendingTicket()
    {
        var pending = _store.ConsumePendingSelection();
        if (pending is not int id)
            return;

        ResetFilters();
        ApplyFilter(id);
        if (TicketsList.SelectedItem is not null)
            TicketsList.ScrollIntoView(TicketsList.SelectedItem);
    }

    private void OnStoreChanged(object? sender, EventArgs e) => ApplyFilter(null);

    private void ResetFilters()
    {
        _filtersReady = false;
        SearchBox.Text = "";
        StatusFilter.SelectedIndex = 0;
        PriorityFilter.SelectedIndex = 0;
        MineOnly.IsChecked = false;
        _filtersReady = true;
    }

    private void FilterSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_filtersReady && !_syncing)
            ApplyFilter(null);
    }

    private void MineOnly_Changed(object sender, RoutedEventArgs e)
    {
        if (_filtersReady && !_syncing)
            ApplyFilter(null);
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_filtersReady && args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            ApplyFilter(null);
    }

    private void ApplyFilter(int? preferredId)
    {
        var selectedId = preferredId ?? (TicketsList.SelectedItem as Ticket)?.Id;
        IEnumerable<Ticket> query = _store.Tickets;
        var text = SearchBox.Text.Trim();

        if (text.Length > 0)
        {
            query = query.Where(ticket =>
                ticket.Code.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || ticket.Title.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || ticket.Description.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || ticket.Requester.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || ticket.Department.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || ticket.Asset.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || (ticket.Assignee?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || ticket.CategoryLabel.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || ticket.StatusLabel.Contains(text, StringComparison.CurrentCultureIgnoreCase));
        }

        if (StatusFilter.SelectedItem is LabeledOption<TicketStatus> { Value: TicketStatus status })
            query = query.Where(ticket => ticket.Status == status);

        if (PriorityFilter.SelectedItem is LabeledOption<TicketPriority> { Value: TicketPriority priority })
            query = query.Where(ticket => ticket.Priority == priority);

        if (MineOnly.IsChecked == true)
        {
            query = query.Where(ticket =>
                string.Equals(ticket.Assignee, _store.CurrentTechnician, StringComparison.CurrentCultureIgnoreCase));
        }

        var result = query.ToList();

        _syncing = true;
        FilteredTickets.Clear();
        foreach (var ticket in result)
            FilteredTickets.Add(ticket);

        var selected = selectedId is int id
            ? FilteredTickets.FirstOrDefault(ticket => ticket.Id == id)
            : null;
        TicketsList.SelectedItem = selected ?? FilteredTickets.FirstOrDefault();
        _syncing = false;

        ShowDetail(TicketsList.SelectedItem as Ticket);

        CountText.Text = result.Count == 1 ? "1 ticket" : $"{result.Count} tickets";
        var hasItems = result.Count > 0;
        TicketsList.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        EmptyList.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;

        if (hasItems)
            return;

        var any = _store.Tickets.Count > 0;
        EmptyListTitle.Text = any ? "Nada coincide con el filtro" : "No hay tickets";
        EmptyListText.Text = any
            ? "Prueba con otro estado, prioridad o texto de búsqueda."
            : "Crea el primer ticket para que el equipo de TI lo atienda.";
        EmptyListButton.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
    }

    private void TicketsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
            return;

        ShowDetail(TicketsList.SelectedItem as Ticket);
    }

    private void ShowDetail(Ticket? ticket)
    {
        if (ticket is null)
        {
            DetailRoot.Visibility = Visibility.Collapsed;
            EmptyDetail.Visibility = Visibility.Visible;
            _shownId = null;
            return;
        }

        EmptyDetail.Visibility = Visibility.Collapsed;
        DetailRoot.Visibility = Visibility.Visible;
        DetailRoot.DataContext = ticket;

        if (_shownId != ticket.Id)
        {
            NoteBox.Text = "";
            DetailInfo.IsOpen = false;
            _shownId = ticket.Id;
        }

        _syncing = true;
        StatusBox.SelectedItem = _statuses.First(option => option.Value == ticket.Status);
        PriorityBox.SelectedItem = _priorities.First(option => option.Value == ticket.Priority);

        var names = new List<string> { Unassigned };
        names.AddRange(_store.Technicians);
        if (!string.IsNullOrWhiteSpace(ticket.Assignee) && !names.Contains(ticket.Assignee))
            names.Add(ticket.Assignee);

        AssigneeBox.ItemsSource = names;
        AssigneeBox.SelectedItem = string.IsNullOrWhiteSpace(ticket.Assignee) ? Unassigned : ticket.Assignee;
        AssignMeButton.IsEnabled = !string.Equals(ticket.Assignee, _store.CurrentTechnician, StringComparison.CurrentCultureIgnoreCase);
        EmptyNotes.Visibility = ticket.Comments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ResolvedBlock.Visibility = ticket.ResolvedAt is null ? Visibility.Collapsed : Visibility.Visible;
        ResolvedText.Text = ticket.ResolvedAt is DateTimeOffset closed ? TimeText.Long(closed) : "";
        _syncing = false;
    }

    private void StatusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || DetailRoot.DataContext is not Ticket ticket)
            return;
        if (StatusBox.SelectedItem is not LabeledOption<TicketStatus> { Value: TicketStatus status })
            return;

        _store.SetStatus(ticket, status);
    }

    private void PriorityBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || DetailRoot.DataContext is not Ticket ticket)
            return;
        if (PriorityBox.SelectedItem is not LabeledOption<TicketPriority> { Value: TicketPriority priority })
            return;

        _store.SetPriority(ticket, priority);
    }

    private void AssigneeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || DetailRoot.DataContext is not Ticket ticket)
            return;
        if (AssigneeBox.SelectedItem is not string name)
            return;

        _store.Assign(ticket, name == Unassigned ? null : name);
    }

    private void AssignMe_Click(object sender, RoutedEventArgs e)
    {
        if (DetailRoot.DataContext is Ticket ticket)
            _store.Take(ticket);
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        if (DetailRoot.DataContext is not Ticket ticket)
            return;

        if (!_store.AddComment(ticket, NoteBox.Text))
        {
            DetailInfo.Severity = InfoBarSeverity.Warning;
            DetailInfo.Title = "Nota vacía";
            DetailInfo.Message = "Escribe una nota antes de agregarla.";
            DetailInfo.IsOpen = true;
            return;
        }

        NoteBox.Text = "";
        DetailInfo.IsOpen = false;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            DetailScroll.ChangeView(null, DetailScroll.ScrollableHeight, null));
    }

    private void NoteBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        if (e.Key != VirtualKey.Enter || !ctrl.HasFlag(CoreVirtualKeyStates.Down))
            return;

        e.Handled = true;
        AddNote_Click(sender, e);
    }

    private void NewTicket_Click(object sender, RoutedEventArgs e) => AppNavigator.Go("nuevo");
}
