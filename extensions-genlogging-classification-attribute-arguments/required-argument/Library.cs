using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Logging;

// A classification attribute that takes the classification's value as an argument.
public sealed class SensitiveAttribute(string level) : DataClassificationAttribute(new DataClassification("Repro", level));

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Looked up")]
    public static partial void LookedUp(ILogger logger, [Sensitive("Health")] string diagnosis);
}
