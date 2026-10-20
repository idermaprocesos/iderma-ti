using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using MesaAyudaTI.Models;
using Windows.Storage;

namespace MesaAyudaTI.Services;

public sealed partial class TicketStore : ObservableObject
{
    public static TicketStore Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _dataFilePath;
    private bool _suspend;
    private int? _pendingSelectionId;

    public ObservableCollection<Ticket> Tickets { get; } = new();
    public ObservableCollection<string> Technicians { get; } = new();

    [ObservableProperty]
    private string _currentTechnician = "Andrés Molina";

    public string DataFilePath => _dataFilePath;

    public string? LastError { get; private set; }

    public event EventHandler? Changed;

    private TicketStore()
    {
        _dataFilePath = Path.Combine(GetDataDirectory(), "mesa-ayuda.json");
        Load();
    }

    public void RequestSelection(int ticketId) => _pendingSelectionId = ticketId;

    public int? ConsumePendingSelection()
    {
        var id = _pendingSelectionId;
        _pendingSelectionId = null;
        return id;
    }

    public Ticket Create(
        string title,
        string description,
        string requester,
        string department,
        string asset,
        TicketCategory category,
        TicketPriority priority)
    {
        var now = DateTimeOffset.Now;
        var ticket = new Ticket
        {
            Id = NextId(),
            Title = title.Trim(),
            Description = description.Trim(),
            Requester = requester.Trim(),
            Department = department.Trim(),
            Asset = asset.Trim(),
            Category = category,
            Priority = priority,
            Status = TicketStatus.Nuevo,
            CreatedAt = now,
            UpdatedAt = now
        };

        Tickets.Insert(0, ticket);
        SaveAndNotify();
        return ticket;
    }

    public void SetStatus(Ticket ticket, TicketStatus status)
    {
        if (ticket.Status == status)
            return;

        ticket.Status = status;
        ticket.UpdatedAt = DateTimeOffset.Now;
        ticket.ResolvedAt = status is TicketStatus.Resuelto or TicketStatus.Cerrado
            ? DateTimeOffset.Now
            : null;
        SaveAndNotify();
    }

    public void SetPriority(Ticket ticket, TicketPriority priority)
    {
        if (ticket.Priority == priority)
            return;

        ticket.Priority = priority;
        ticket.UpdatedAt = DateTimeOffset.Now;
        SaveAndNotify();
    }

    public void Assign(Ticket ticket, string? assignee)
    {
        assignee = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
        if (string.Equals(ticket.Assignee, assignee, StringComparison.Ordinal))
            return;

        ticket.Assignee = assignee;
        ticket.UpdatedAt = DateTimeOffset.Now;
        SaveAndNotify();
    }

    public void Take(Ticket ticket)
    {
        var name = CurrentTechnician.Trim();
        if (name.Length == 0)
            return;

        var known = Technicians.Any(t => string.Equals(t, name, StringComparison.CurrentCultureIgnoreCase));
        if (!known)
            Technicians.Add(name);

        var canonical = Technicians.First(t => string.Equals(t, name, StringComparison.CurrentCultureIgnoreCase));
        var status = ticket.Status == TicketStatus.Nuevo ? TicketStatus.EnProgreso : ticket.Status;
        var changed = !known
            || !string.Equals(ticket.Assignee, canonical, StringComparison.Ordinal)
            || ticket.Status != status;

        ticket.Assignee = canonical;
        ticket.Status = status;
        if (!changed)
            return;

        ticket.UpdatedAt = DateTimeOffset.Now;
        SaveAndNotify();
    }

    public bool AddComment(Ticket ticket, string body)
    {
        body = body.Trim();
        if (body.Length == 0)
            return false;

        ticket.Comments.Add(new TicketComment
        {
            Author = CurrentTechnician,
            Body = body,
            CreatedAt = DateTimeOffset.Now
        });
        ticket.UpdatedAt = DateTimeOffset.Now;
        ticket.NotifyNotesChanged();
        SaveAndNotify();
        return true;
    }

    public bool TryAddTechnician(string name, out string? error)
    {
        name = Collapse(name);
        if (name.Length < 2)
        {
            error = "Escribe el nombre del técnico.";
            return false;
        }

        if (Technicians.Any(t => string.Equals(t, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            error = "Esa persona ya está en el equipo.";
            return false;
        }

        Technicians.Add(name);
        SaveAndNotify();
        error = null;
        return true;
    }

    public string? WhyCannotRemove(string name)
    {
        if (string.Equals(name, CurrentTechnician, StringComparison.CurrentCultureIgnoreCase))
            return "Estás usando esta sesión. Cambia el técnico en Preferencias antes de quitarlo.";

        var open = Tickets.Count(t => t.IsOpen && string.Equals(t.Assignee, name, StringComparison.CurrentCultureIgnoreCase));
        if (open == 1)
            return $"{name} tiene 1 ticket abierto. Reasígnalo antes de quitarlo del equipo.";
        if (open > 1)
            return $"{name} tiene {open} tickets abiertos. Reasígnalos antes de quitarlo del equipo.";

        return null;
    }

    public bool TryRemoveTechnician(string name, out string? error)
    {
        error = WhyCannotRemove(name);
        if (error is not null)
            return false;

        var existing = Technicians.FirstOrDefault(t => string.Equals(t, name, StringComparison.CurrentCultureIgnoreCase));
        if (existing is null)
        {
            error = "Esa persona ya no está en el equipo.";
            return false;
        }

        Technicians.Remove(existing);
        SaveAndNotify();
        error = null;
        return true;
    }

    public void SetCurrentTechnician(string name)
    {
        name = Collapse(name);
        if (name.Length < 2)
            return;

        var existing = Technicians.FirstOrDefault(t => string.Equals(t, name, StringComparison.CurrentCultureIgnoreCase));
        if (existing is null)
            Technicians.Add(name);
        else
            name = existing;

        if (string.Equals(CurrentTechnician, name, StringComparison.Ordinal))
        {
            SaveAndNotify();
            return;
        }

        CurrentTechnician = name;
    }

    public void ResetToSample()
    {
        _suspend = true;
        Tickets.Clear();
        Technicians.Clear();
        ApplySample();
        _suspend = false;
        SaveAndNotify();
    }

    partial void OnCurrentTechnicianChanged(string value)
    {
        if (!_suspend)
            SaveAndNotify();
    }

    private int NextId() => Tickets.Count == 0 ? 1 : Tickets.Max(t => t.Id) + 1;

    private void Load()
    {
        _suspend = true;
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                ApplySample();
                _suspend = false;
                Save();
                return;
            }

            var json = File.ReadAllText(_dataFilePath);
            var snapshot = JsonSerializer.Deserialize<Snapshot>(json, JsonOptions);
            if (snapshot is null)
                throw new InvalidDataException("El archivo de tickets está vacío.");

            ApplySnapshot(snapshot);
            LastError = null;
        }
        catch (Exception ex)
        {
            Tickets.Clear();
            Technicians.Clear();
            ApplySample();
            LastError = "No se pudieron leer los tickets guardados. Se cargó un conjunto de ejemplo. " + ex.Message;
            TryBackupCorruptFile();
        }
        finally
        {
            _suspend = false;
        }
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        foreach (var name in snapshot.Technicians.Where(n => !string.IsNullOrWhiteSpace(n)).Select(Collapse).Distinct(StringComparer.CurrentCultureIgnoreCase))
            Technicians.Add(name);

        if (Technicians.Count == 0)
            ApplyDefaultTechnicians();

        var technician = Collapse(snapshot.CurrentTechnician);
        CurrentTechnician = Technicians.FirstOrDefault(t => string.Equals(t, technician, StringComparison.CurrentCultureIgnoreCase))
            ?? Technicians[0];

        foreach (var ticket in snapshot.Tickets.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id))
        {
            ticket.Comments ??= new ObservableCollection<TicketComment>();
            Tickets.Add(ticket);
        }
    }

    private void ApplySample()
    {
        ApplyDefaultTechnicians();
        CurrentTechnician = "Andrés Molina";

        var now = DateTimeOffset.Now;
        AddSample(1, "Impresora de recepción no imprime etiquetas",
            "Desde esta mañana la impresora de etiquetas no responde. Las citas se están anotando a mano y la cola de recepción está creciendo.",
            "Laura Méndez", "Recepción", "IMP-REC-01", TicketCategory.Impresion, TicketPriority.Critica, TicketStatus.Nuevo,
            null, now.AddMinutes(-35), null);

        AddSample(2, "Sin acceso al sistema de historia clínica",
            "El doctor no puede abrir la historia de los pacientes de la tarde. El inicio de sesión dice que el usuario no tiene permiso.",
            "Dr. Pablo Herrera", "Consultorios", "PC-CON-04", TicketCategory.Acceso, TicketPriority.Alta, TicketStatus.EnProgreso,
            "Andrés Molina", now.AddHours(-3), null,
            Note("Andrés Molina", "Reinicié la sesión y el perfil no tiene el grupo HC_CONSULTA. Pedí el alta en el directorio.", now.AddHours(-2)));

        AddSample(3, "El Wi-Fi del quirófano se corta",
            "Durante el procedimiento se cae la red unos segundos. El equipo de cámaras pierde la conexión y hay que reconectar.",
            "Enf. Sofía León", "Quirófano", "AP-QX-02", TicketCategory.Red, TicketPriority.Alta, TicketStatus.EnEspera,
            "Camila Ríos", now.AddHours(-6), null,
            Note("Camila Ríos", "El punto de acceso AP-QX-02 se reinicia solo. Dejé uno de reemplazo pendiente de autorización.", now.AddHours(-5)));

        AddSample(4, "El correo de administración no envía",
            "Los mensajes salen de la bandeja y regresan con un error de retransmisión. Recibir sí funciona.",
            "Marta Quintero", "Administración", "PC-ADM-02", TicketCategory.Correo, TicketPriority.Media, TicketStatus.Nuevo,
            null, now.AddHours(-8), null);

        AddSample(5, "Alta de usuario para la nueva recepcionista",
            "Mañana entra Valeria Campos. Necesita correo, sistema de citas e impresora de etiquetas.",
            "Laura Méndez", "Recepción", "PC-REC-03", TicketCategory.Acceso, TicketPriority.Media, TicketStatus.EnProgreso,
            "Diego Herrera", now.AddDays(-1), null,
            Note("Diego Herrera", "Correo creado. Falta agregarla al grupo de citas y probar la impresora.", now.AddHours(-20)));

        AddSample(6, "No abre el software de cámaras",
            "En el consultorio 2 el programa de las cámaras se queda en la pantalla de inicio y no muestra imagen.",
            "Dra. Elena Vargas", "Consultorios", "PC-CON-02", TicketCategory.Software, TicketPriority.Alta, TicketStatus.Nuevo,
            null, now.AddMinutes(-80), null);

        AddSample(7, "El monitor del laboratorio parpadea",
            "La pantalla se pone en negro cada pocos minutos. Ya se probó con otro cable y sigue igual en ese equipo.",
            "Julián Paredes", "Laboratorio", "PC-LAB-01", TicketCategory.Hardware, TicketPriority.Baja, TicketStatus.Resuelto,
            "Camila Ríos", now.AddDays(-2), now.AddDays(-1).AddHours(2),
            Note("Camila Ríos", "Se cambió el cable DisplayPort y el parpadeo se detuvo. Quedó en observación.", now.AddDays(-1).AddHours(2)));
    }

    private void AddSample(
        int id,
        string title,
        string description,
        string requester,
        string department,
        string asset,
        TicketCategory category,
        TicketPriority priority,
        TicketStatus status,
        string? assignee,
        DateTimeOffset created,
        DateTimeOffset? resolved,
        params TicketComment[] notes)
    {
        var updated = notes.Length == 0 ? created : notes.Max(n => n.CreatedAt);
        if (resolved is DateTimeOffset closed && closed > updated)
            updated = closed;

        Tickets.Add(new Ticket
        {
            Id = id,
            Title = title,
            Description = description,
            Requester = requester,
            Department = department,
            Asset = asset,
            Category = category,
            Priority = priority,
            Status = status,
            Assignee = assignee,
            CreatedAt = created,
            UpdatedAt = updated,
            ResolvedAt = resolved,
            Comments = new ObservableCollection<TicketComment>(notes)
        });
    }

    private static TicketComment Note(string author, string body, DateTimeOffset created) => new()
    {
        Author = author,
        Body = body,
        CreatedAt = created
    };

    private void ApplyDefaultTechnicians()
    {
        Technicians.Add("Andrés Molina");
        Technicians.Add("Camila Ríos");
        Technicians.Add("Diego Herrera");
    }

    private void SaveAndNotify()
    {
        if (_suspend)
            return;

        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        if (_suspend)
            return;

        try
        {
            var snapshot = new Snapshot
            {
                CurrentTechnician = CurrentTechnician,
                Technicians = Technicians.ToList(),
                Tickets = Tickets.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id).ToList()
            };

            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temp = _dataFilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(snapshot, JsonOptions));
            File.Move(temp, _dataFilePath, overwrite: true);
            LastError = null;
        }
        catch (Exception ex)
        {
            LastError = "No se pudieron guardar los cambios en este equipo. " + ex.Message;
        }
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            if (File.Exists(_dataFilePath))
                File.Copy(_dataFilePath, _dataFilePath + ".bak", overwrite: true);
        }
        catch
        {
            // El respaldo es opcional; la app sigue con los datos de ejemplo.
        }
    }

    private static string GetDataDirectory()
    {
        try
        {
            var packaged = ApplicationData.Current.LocalFolder.Path;
            if (!string.IsNullOrWhiteSpace(packaged))
                return packaged;
        }
        catch
        {
            // Sin identidad de paquete se usa la carpeta local del usuario.
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MesaAyudaTI");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static string Collapse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed class Snapshot
    {
        public string CurrentTechnician { get; set; } = "";
        public List<string> Technicians { get; set; } = new();
        public List<Ticket> Tickets { get; set; } = new();
    }
}
