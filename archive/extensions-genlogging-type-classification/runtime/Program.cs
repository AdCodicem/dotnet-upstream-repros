using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(logging => logging.EnableRedaction().AddProvider(new TagPrinter()));
services.AddRedaction(redaction => redaction.SetFallbackRedactor<MarkingRedactor>()); // every classification is redacted
using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Repro");

var email = new Email("jane@example.com");

Console.WriteLine("1. [PersonalData] on the parameter (redacted, as expected):");
Log.ParameterClassified(logger, email);

Console.WriteLine("2. [PersonalData] on the parameter's type (expected redacted):");
Log.TypeClassified(logger, email);

Console.WriteLine("3. A record logged whole whose property's type is [PersonalData] (expected LOGGEN035, as for a [PersonalData] property):");
Log.RecordWithTypeClassifiedMember(logger, new Contact(email));

// A classification attribute, as the docs describe it. DataClassificationAttribute's AttributeUsage allows Class and Struct.
public sealed class PersonalDataAttribute() : DataClassificationAttribute(new DataClassification("Repro", "PersonalData"));

[PersonalData]
public readonly struct Email(string value)
{
    public string Value => value;

    public override string ToString() => value;
}

public sealed record Contact(Email Email);

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Signed up")]
    public static partial void ParameterClassified(ILogger logger, [PersonalData] Email email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed up")]
    public static partial void TypeClassified(ILogger logger, Email email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed up {Contact}")]
    public static partial void RecordWithTypeClassifiedMember(ILogger logger, Contact contact);
}

// Writes "<redacted>" in place of any classified value.
internal sealed class MarkingRedactor : Redactor
{
    private const string Mark = "<redacted>";

    public override int GetRedactedLength(ReadOnlySpan<char> input) => Mark.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        Mark.AsSpan().CopyTo(destination);
        return Mark.Length;
    }
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

    public void Dispose()
    {
    }
}
