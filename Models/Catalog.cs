namespace MesaAyudaTI.Models;

public static class Catalog
{
    public static IReadOnlyList<string> Departments { get; } =
    [
        "Recepción",
        "Consultorios",
        "Quirófano",
        "Administración",
        "Laboratorio",
        "Dirección"
    ];

    public static string Status(TicketStatus status) => status switch
    {
        TicketStatus.Nuevo => "Nuevo",
        TicketStatus.EnProgreso => "En progreso",
        TicketStatus.EnEspera => "En espera",
        TicketStatus.Resuelto => "Resuelto",
        TicketStatus.Cerrado => "Cerrado",
        _ => status.ToString()
    };

    public static string Priority(TicketPriority priority) => priority switch
    {
        TicketPriority.Baja => "Baja",
        TicketPriority.Media => "Media",
        TicketPriority.Alta => "Alta",
        TicketPriority.Critica => "Crítica",
        _ => priority.ToString()
    };

    public static string Category(TicketCategory category) => category switch
    {
        TicketCategory.Hardware => "Hardware",
        TicketCategory.Software => "Software",
        TicketCategory.Red => "Red",
        TicketCategory.Acceso => "Accesos",
        TicketCategory.Impresion => "Impresión",
        TicketCategory.Correo => "Correo",
        TicketCategory.Otro => "Otro",
        _ => category.ToString()
    };

    public static List<LabeledOption<TicketStatus>> StatusChoices() =>
    [
        Option(TicketStatus.Nuevo, Status(TicketStatus.Nuevo)),
        Option(TicketStatus.EnProgreso, Status(TicketStatus.EnProgreso)),
        Option(TicketStatus.EnEspera, Status(TicketStatus.EnEspera)),
        Option(TicketStatus.Resuelto, Status(TicketStatus.Resuelto)),
        Option(TicketStatus.Cerrado, Status(TicketStatus.Cerrado))
    ];

    public static List<LabeledOption<TicketStatus>> StatusFilters() =>
        [All<TicketStatus>("Todos los estados"), .. StatusChoices()];

    public static List<LabeledOption<TicketPriority>> PriorityChoices() =>
    [
        Option(TicketPriority.Critica, Priority(TicketPriority.Critica)),
        Option(TicketPriority.Alta, Priority(TicketPriority.Alta)),
        Option(TicketPriority.Media, Priority(TicketPriority.Media)),
        Option(TicketPriority.Baja, Priority(TicketPriority.Baja))
    ];

    public static List<LabeledOption<TicketPriority>> PriorityFilters() =>
        [All<TicketPriority>("Todas las prioridades"), .. PriorityChoices()];

    public static List<LabeledOption<TicketCategory>> CategoryChoices() =>
    [
        Option(TicketCategory.Hardware, Category(TicketCategory.Hardware)),
        Option(TicketCategory.Software, Category(TicketCategory.Software)),
        Option(TicketCategory.Red, Category(TicketCategory.Red)),
        Option(TicketCategory.Acceso, Category(TicketCategory.Acceso)),
        Option(TicketCategory.Impresion, Category(TicketCategory.Impresion)),
        Option(TicketCategory.Correo, Category(TicketCategory.Correo)),
        Option(TicketCategory.Otro, Category(TicketCategory.Otro))
    ];

    private static LabeledOption<T> Option<T>(T value, string label) where T : struct =>
        new() { Value = value, Label = label };

    private static LabeledOption<T> All<T>(string label) where T : struct =>
        new() { Value = null, Label = label };
}
