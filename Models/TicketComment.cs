namespace MesaAyudaTI.Models;

public sealed class TicketComment
{
    public string Author { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
