using Microsoft.Extensions.Logging;

// Next is declared on the base class, and its type is the derived class.
public class Node
{
    public Leaf? Next { get; init; }
}

public sealed class Leaf : Node;

public sealed class Holder
{
    public Leaf? Leaf { get; init; }
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received")]
    public static partial void Received(ILogger logger, [LogProperties(Transitive = true)] Holder holder);
}
