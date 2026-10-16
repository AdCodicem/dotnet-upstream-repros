using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(logging => logging.EnableRedaction().AddProvider(new TagPrinter()));
services.AddRedaction(redaction => redaction.SetFallbackRedactor<MarkingRedactor>()); // every classification is redacted
using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Repro");

var order = new Order { Id = 42, Card = new Card { Number = "4111 1111 1111 1111", Expiry = "12/29" } };
var receipt = new Receipt { Card = order.Card };
var account = new Account { Name = "jane", Password = "hunter2" };
var shipment = new Shipment { Address = new Address { City = "Paris", Geo = new Geo { Latitude = 48.85, Longitude = 2.35 } } };

Console.WriteLine("A. [PersonalData] property, [LogProperties] (redacted, as expected):");
Log.OrderPlaced(logger, order);

Console.WriteLine("B. Same property, [LogProperties(Transitive = true)] (expected order.Card.* redacted):");
Log.OrderPlacedTransitive(logger, order);

Console.WriteLine("C. [PersonalData] and [LogProperties] on the same property (expected receipt.Card.* redacted, or LOGGEN017 as on a parameter):");
Log.Receipted(logger, receipt);

Console.WriteLine("D. [LogPropertyIgnore] on a property, [LogProperties] (left out, as expected):");
Log.SignedIn(logger, account);

Console.WriteLine("E. Same property, [LogProperties(Transitive = true)] (expected no account.Password):");
Log.SignedInTransitive(logger, account);

Console.WriteLine("F. [LogProperties(Transitive = true)] on a property, [LogProperties] on the parameter (expected shipment.Address.Geo.Latitude and .Longitude):");
Log.Shipped(logger, shipment);

public sealed class PersonalDataAttribute() : DataClassificationAttribute(new DataClassification("Repro", "PersonalData"));

public sealed class Card
{
    public string Number { get; init; } = "";
    public string Expiry { get; init; } = "";
    public override string ToString() => $"{Number} {Expiry}";
}

public sealed class Order
{
    public int Id { get; init; }

    [PersonalData]
    public Card? Card { get; init; }
}

public sealed class Receipt
{
    [PersonalData]
    [LogProperties]
    public Card? Card { get; init; }
}

public sealed class Account
{
    public string Name { get; init; } = "";

    [LogPropertyIgnore]
    public string Password { get; init; } = "";
}

public sealed class Geo
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public override string ToString() => "Geo.ToString()";
}

public sealed class Address
{
    public string City { get; init; } = "";
    public Geo? Geo { get; init; }
    public override string ToString() => "Address.ToString()";
}

public sealed class Shipment
{
    [LogProperties(Transitive = true)]
    public Address? Address { get; init; }
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order placed")]
    public static partial void OrderPlaced(ILogger logger, [LogProperties] Order order);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order placed")]
    public static partial void OrderPlacedTransitive(ILogger logger, [LogProperties(Transitive = true)] Order order);

    [LoggerMessage(Level = LogLevel.Information, Message = "Receipt sent")]
    public static partial void Receipted(ILogger logger, [LogProperties] Receipt receipt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed in")]
    public static partial void SignedIn(ILogger logger, [LogProperties] Account account);

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed in")]
    public static partial void SignedInTransitive(ILogger logger, [LogProperties(Transitive = true)] Account account);

    [LoggerMessage(Level = LogLevel.Information, Message = "Shipped")]
    public static partial void Shipped(ILogger logger, [LogProperties] Shipment shipment);
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
