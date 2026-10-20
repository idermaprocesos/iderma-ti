namespace MesaAyudaTI.Models;

public sealed class LabeledOption<T> where T : struct
{
    public T? Value { get; init; }
    public required string Label { get; init; }
}
