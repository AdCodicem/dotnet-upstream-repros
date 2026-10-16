using Microsoft.Extensions.Logging;

// The same cycle, with Next declared on Leaf itself: LOGGEN021, as expected.
public sealed class Leaf
{
    public Leaf? Next { get; init; }
}

public sealed class Holder
{
    public Leaf? Leaf { get; init; }
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received")]
    public static partial void Received(ILogger logger, [LogProperties(Transitive = true)] Holder holder);
}
