using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MesaAyudaTI.Models;

public partial class Ticket : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _requester = "";

    [ObservableProperty]
    private string _department = "";

    [ObservableProperty]
    private string _asset = "";

    [ObservableProperty]
    private TicketCategory _category;

    [ObservableProperty]
    private TicketPriority _priority = TicketPriority.Media;

    [ObservableProperty]
    private TicketStatus _status;

    [ObservableProperty]
    private string? _assignee;

    public DateTimeOffset CreatedAt { get; set; }

    [ObservableProperty]
    private DateTimeOffset _updatedAt;

    public DateTimeOffset? ResolvedAt { get; set; }

    public ObservableCollection<TicketComment> Comments { get; set; } = new();

    [JsonIgnore]
    public string Code => $"TI-{Id:0000}";

    [JsonIgnore]
    public string StatusLabel => Catalog.Status(Status);

    [JsonIgnore]
    public string PriorityLabel => Catalog.Priority(Priority);

    [JsonIgnore]
    public string CategoryLabel => Catalog.Category(Category);

    [JsonIgnore]
    public string AssigneeLabel => string.IsNullOrWhiteSpace(Assignee) ? "Sin asignar" : Assignee;

    [JsonIgnore]
    public string AssetLabel => string.IsNullOrWhiteSpace(Asset) ? "No indicado" : Asset;

    [JsonIgnore]
    public string MetaLine => $"{PriorityLabel} · {Department} · {Requester}";

    [JsonIgnore]
    public bool IsOpen => Status is TicketStatus.Nuevo or TicketStatus.EnProgreso or TicketStatus.EnEspera;

    [JsonIgnore]
    public string NotesLabel => Comments.Count switch
    {
        0 => "Sin notas",
        1 => "1 nota",
        _ => $"{Comments.Count} notas"
    };

    public void NotifyNotesChanged() => OnPropertyChanged(nameof(NotesLabel));

    partial void OnStatusChanged(TicketStatus value)
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(IsOpen));
    }

    partial void OnPriorityChanged(TicketPriority value)
    {
        OnPropertyChanged(nameof(PriorityLabel));
        OnPropertyChanged(nameof(MetaLine));
    }

    partial void OnCategoryChanged(TicketCategory value) => OnPropertyChanged(nameof(CategoryLabel));

    partial void OnAssigneeChanged(string? value) => OnPropertyChanged(nameof(AssigneeLabel));

    partial void OnAssetChanged(string value) => OnPropertyChanged(nameof(AssetLabel));

    partial void OnDepartmentChanged(string value) => OnPropertyChanged(nameof(MetaLine));

    partial void OnRequesterChanged(string value) => OnPropertyChanged(nameof(MetaLine));
}
