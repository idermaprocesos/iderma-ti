using System.Globalization;

namespace MesaAyudaTI.Services;

public static class TimeText
{
    private static readonly CultureInfo Culture = new("es-ES");

    public static string Long(DateTimeOffset value) =>
        value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Culture);

    public static string Relative(DateTimeOffset value)
    {
        var span = DateTimeOffset.Now - value;
        if (span < TimeSpan.Zero || span.TotalMinutes < 1)
            return "ahora";

        if (span.TotalMinutes < 60)
        {
            var minutes = (int)span.TotalMinutes;
            return minutes == 1 ? "hace 1 minuto" : $"hace {minutes} minutos";
        }

        if (span.TotalHours < 24)
        {
            var hours = (int)span.TotalHours;
            return hours == 1 ? "hace 1 hora" : $"hace {hours} horas";
        }

        if (span.TotalDays < 7)
        {
            var days = (int)span.TotalDays;
            return days == 1 ? "hace 1 día" : $"hace {days} días";
        }

        return value.ToLocalTime().ToString("d MMM", Culture);
    }

    public static string Today()
    {
        var text = DateTime.Now.ToString("dddd d 'de' MMMM", Culture);
        return text.Length == 0 ? text : char.ToUpper(text[0], Culture) + text[1..];
    }

    public static string Greeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 12)
            return "Buenos días";
        if (hour < 19)
            return "Buenas tardes";
        return "Buenas noches";
    }
}
