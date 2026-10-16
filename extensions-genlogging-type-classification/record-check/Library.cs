using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Logging;

public sealed class PersonalDataAttribute() : DataClassificationAttribute(new DataClassification("Repro", "PersonalData"));

[PersonalData]
public readonly struct Email(string value)
{
    public string Value => value;

    public override string ToString() => value;
}

[PersonalData]
public sealed record Card(string Number);

// Classified on the property: LOGGEN035.
public sealed record ClassifiedContact([property: PersonalData] Email Email);

// Classified on the property's type, a record: LOGGEN035.
public sealed record Wallet(Card Card);

// Classified on the property's type, a struct: no diagnostic, and the record's ToString() writes the address in clear.
public sealed record Contact(Email Email);

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Signed up {Contact}")]
    public static partial void PropertyClassified(ILogger logger, ClassifiedContact contact);

    [LoggerMessage(Level = LogLevel.Information, Message = "Paid with {Wallet}")]
    public static partial void RecordTypeClassified(ILogger logger, Wallet wallet);

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed up {Contact}")]
    public static partial void StructTypeClassified(ILogger logger, Contact contact);
}
