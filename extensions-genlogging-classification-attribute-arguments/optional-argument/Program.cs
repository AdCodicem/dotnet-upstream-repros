using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(logging => logging.EnableRedaction().AddProvider(new TagPrinter()));
// Health goes to MarkingRedactor, any other classification to FallbackRedactor.
services.AddRedaction(redaction => redaction
    .SetRedactor<MarkingRedactor>(new DataClassificationSet(new DataClassification("Repro", "Health")))
    .SetFallbackRedactor<FallbackRedactor>());
using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Repro");
Log.LookedUp(logger, "diabetes");

// The same attribute as in required-argument/, with a default value for its argument.
public sealed class SensitiveAttribute(string level = "Public") : DataClassificationAttribute(new DataClassification("Repro", level));

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Looked up")]
    public static partial void LookedUp(ILogger logger, [Sensitive("Health")] string diagnosis);
}

// Writes "<redacted>" in place of a Health value.
internal sealed class MarkingRedactor : Redactor
{
    private const string Mark = "<redacted>";
    public override int GetRedactedLength(ReadOnlySpan<char> input) => Mark.Length;
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination) { Mark.AsSpan().CopyTo(destination); return Mark.Length; }
}

// Writes "<fallback>" in place of any other classified value.
internal sealed class FallbackRedactor : Redactor
{
    private const string Mark = "<fallback>";
    public override int GetRedactedLength(ReadOnlySpan<char> input) => Mark.Length;
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination) { Mark.AsSpan().CopyTo(destination); return Mark.Length; }
}

// Prints the tags each log record reaches a provider with, after redaction.
internal sealed class TagPrinter : ILoggerProvider, ILogger
{
    public ILogger CreateLogger(string categoryName) => this;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var tags = ((IEnumerable<KeyValuePair<string, object?>>)state!).Where(tag => tag.Key != "{OriginalFormat}");
        Console.WriteLine("   " + string.Join(", ", tags.Select(tag => $"\"{tag.Key}\":\"{tag.Value}\"")));
    }
    public void Dispose() { }
}
