using Microsoft.Extensions.Logging;

// A property of type System.Type: its graph reaches such a cycle through inherited reflection properties.
public sealed class Message
{
    public Type? Kind { get; init; }
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received")]
    public static partial void Received(ILogger logger, [LogProperties(Transitive = true)] Message message);
}
