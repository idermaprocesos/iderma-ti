namespace MesaAyudaTI.Services;

public static class AppNavigator
{
    public static event Action<string>? NavigateRequested;

    public static void Go(string tag) => NavigateRequested?.Invoke(tag);
}
