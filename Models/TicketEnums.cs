namespace MesaAyudaTI.Models;

public enum TicketStatus
{
    Nuevo,
    EnProgreso,
    EnEspera,
    Resuelto,
    Cerrado
}

public enum TicketPriority
{
    Baja,
    Media,
    Alta,
    Critica
}

public enum TicketCategory
{
    Hardware,
    Software,
    Red,
    Acceso,
    Impresion,
    Correo,
    Otro
}
