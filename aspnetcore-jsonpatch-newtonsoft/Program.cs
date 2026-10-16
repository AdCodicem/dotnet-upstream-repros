using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// The application's contract resolver: camelCase, and the converter of Sku, which carries no [JsonConverter].
var resolver = new AppContractResolver();

// Newtonsoft.Json's defaults, which AddNewtonsoftJson() keeps, but for the resolver.
var defaults = new JsonSerializerSettings { ContractResolver = resolver };

// Settings an application may give MvcNewtonsoftJsonOptions.SerializerSettings.
var tuned = new JsonSerializerSettings
{
    ContractResolver = resolver,
    DateParseHandling = DateParseHandling.None,
    FloatParseHandling = FloatParseHandling.Decimal,
    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
    Converters = { new PercentConverter() },
};

Console.WriteLine(
    $"Microsoft.AspNetCore.JsonPatch {Version(typeof(JsonPatchDocument))}, Newtonsoft.Json {Version(typeof(JsonConvert))}, "
    + $"local UTC offset on 2024-05-01: {TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc))}");

Section("1. ConvertTo reads a value back under settings holding the contract resolver alone, and swallows the converter's exception");
Case("1a. DateTimeOffset, Newtonsoft.Json's default settings", defaults,
    """{"placedAt":"2024-05-01T10:00:00+05:30"}""",
    """[{"op":"replace","path":"/placedAt","value":"2024-05-01T10:00:00+05:30"}]""",
    o => o.PlacedAt.ToString("o", CultureInfo.InvariantCulture));
Case("1b. decimal, Newtonsoft.Json's default settings", defaults,
    """{"total":0.12345678901234567890}""",
    """[{"op":"replace","path":"/total","value":0.12345678901234567890}]""",
    o => o.Total.ToString(CultureInfo.InvariantCulture));
Case("1c. the same two, under DateParseHandling.None and FloatParseHandling.Decimal (no conversion: the value is assignable)", tuned,
    """{"placedAt":"2024-05-01T10:00:00+05:30","total":0.12345678901234567890}""",
    """[{"op":"replace","path":"/placedAt","value":"2024-05-01T10:00:00+05:30"},{"op":"replace","path":"/total","value":0.12345678901234567890}]""",
    o => $"{o.PlacedAt.ToString("o", CultureInfo.InvariantCulture)}, {o.Total.ToString(CultureInfo.InvariantCulture)}");
Case("1d. DateTime, DateTimeZoneHandling.Utc", tuned,
    """{"shippedAt":"2024-05-01T10:00:00+05:30"}""",
    """[{"op":"replace","path":"/shippedAt","value":"2024-05-01T10:00:00+05:30"}]""",
    o => $"{o.ShippedAt.ToString("o", CultureInfo.InvariantCulture)} (Kind {o.ShippedAt.Kind})");
Case("1e. a [JsonConverter] reading reader.Value, FloatParseHandling.Decimal", tuned,
    """{"price":0.12345678901234567890}""",
    """[{"op":"replace","path":"/price","value":0.12345678901234567890}]""",
    o => o.Price.Amount.ToString(CultureInfo.InvariantCulture));
Case("1f. a converter of JsonSerializerSettings.Converters", tuned,
    """{"discount":15}""",
    """[{"op":"replace","path":"/discount","value":15}]""",
    o => o.Discount.Value.ToString(CultureInfo.InvariantCulture));
Case("1g. the exception of the contract resolver's converter", tuned,
    """{"sku":"abc"}""",
    """[{"op":"replace","path":"/sku","value":"abc"}]""",
    o => o.Sku.ToString());

Section("2. CopyTo, TryConvertKey and TryTest ignore the document's contract resolver");
Console.WriteLine($"  JsonConvert.SerializeObject(sku)                -> {JsonConvert.SerializeObject(new Sku("ABCD1234"))}");
Console.WriteLine($"  JsonConvert.SerializeObject(sku, resolver)      -> {JsonConvert.SerializeObject(new Sku("ABCD1234"), defaults)}");
Case("2a. copy (CopyTo)", tuned, null,
    """[{"op":"copy","from":"/sku","path":"/backupSku"}]""",
    o => $"sku {o.Sku}, backupSku {o.BackupSku}");
Case("2b. a dictionary key (TryConvertKey); the value, of the same type, converts", tuned, null,
    """[{"op":"add","path":"/replacements/ABCD1234","value":"EFGH5678"}]""",
    o => string.Join(", ", o.Replacements.Select(p => $"{p.Key} => {p.Value}")));
Case("2c. test (TryTest compares JsonConvert.SerializeObject of both values)", tuned, null,
    """[{"op":"test","path":"/sku","value":"ZZZZZZZZ"}]""",
    o => $"test passed, sku is {o.Sku}");

Section("3. '-' where the list holds no element: ArgumentOutOfRangeException instead of a JsonPatchError");
Case("3a. remove /empty/- (an empty list)", tuned, null, """[{"op":"remove","path":"/empty/-"}]""", o => "applied");
Case("3b. replace /empty/- (an empty list)", tuned, null, """[{"op":"replace","path":"/empty/-","value":1}]""", o => "applied");
Case("3c. copy from /empty/- (an empty list)", tuned, null, """[{"op":"copy","from":"/empty/-","path":"/lines/-"}]""", o => "applied");
Case("3d. test /lines/- (lines is [1,2,3])", tuned, null, """[{"op":"test","path":"/lines/-","value":3}]""", o => "applied");
Case("3e. for comparison, remove /empty/0", tuned, null, """[{"op":"remove","path":"/empty/0"}]""", o => "applied");

Section("4. a missing path or from: ArgumentNullException instead of a JsonPatchError");
Case("4a. replace without path", tuned, null, """[{"op":"replace","value":1}]""", o => "applied");
Case("4b. copy without from", tuned, null, """[{"op":"copy","path":"/total"}]""", o => "applied");

Section("5. move adds the value at path after removing it from from failed: a copy, not a move");
Case("5a. move from a get-only property, errors logged (as ApplyTo(model, ModelState))", tuned, null,
    """[{"op":"move","from":"/reference","path":"/note"}]""",
    o => $"reference {o.Reference}, note {o.Note ?? "null"}", showAfterErrors: true);
Case("5b. the same, ApplyTo(model) without an error callback", tuned, null,
    """[{"op":"move","from":"/reference","path":"/note"}]""",
    o => $"reference {o.Reference}, note {o.Note ?? "null"}", logErrors: false, showAfterErrors: true);
Case("5c. for comparison, remove of the same property", tuned, null,
    """[{"op":"remove","path":"/reference"}]""",
    o => $"reference {o.Reference}, note {o.Note ?? "null"}", showAfterErrors: true);

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"== {title}");
}

static void Case(
    string title,
    JsonSerializerSettings settings,
    string? body,
    string patch,
    Func<Order, string> show,
    bool logErrors = true,
    bool showAfterErrors = false)
{
    Console.WriteLine(title);
    if (body is not null)
    {
        Console.Write($"  body  {body} -> ");
        try
        {
            Console.WriteLine(show(JsonConvert.DeserializeObject<Order>(body, settings)!));
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
        }
    }

    Console.Write($"  patch {patch} -> ");
    var document = JsonConvert.DeserializeObject<JsonPatchDocument<Order>>(patch, settings)!;

    // As MVC's NewtonsoftJsonPatchInputFormatter does.
    document.ContractResolver = settings.ContractResolver!;
    var order = new Order();
    var errors = new List<JsonPatchError>();
    try
    {
        if (logErrors)
        {
            // As MVC's ApplyTo(objectToApplyTo, ModelState) does: errors are logged, not thrown.
            document.ApplyTo(order, errors.Add);
        }
        else
        {
            document.ApplyTo(order);
        }

        var reported = string.Join(" | ", errors.Select(e => $"JsonPatchError: {e.ErrorMessage} (operation path '{e.Operation.path}', from '{e.Operation.from}')"));
        Console.WriteLine(errors.Count == 0 ? show(order) : showAfterErrors ? $"{reported}; then {show(order)}" : reported);
    }
    catch (Exception e)
    {
        Console.WriteLine($"threw {e.GetType().FullName}: {e.Message}{(showAfterErrors ? $"; then {show(order)}" : "")}");
        // The first frame of JSON Patch, where the exception escapes from, past its default error reporter.
        Console.WriteLine($"        {e.StackTrace?.Split('\n').Select(f => f.Trim()).FirstOrDefault(f => f.Contains("Microsoft.AspNetCore.JsonPatch") && !f.Contains("ErrorReporter"))}");
    }
}

static string? Version(Type type) => type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];

public sealed class Order
{
    public DateTimeOffset PlacedAt { get; set; }

    public decimal Total { get; set; }

    public DateTime ShippedAt { get; set; }

    public Money Price { get; set; }

    public Percent Discount { get; set; }

    public Sku Sku { get; set; } = new("ABCD1234");

    public Sku BackupSku { get; set; }

    public List<int> Lines { get; set; } = [1, 2, 3];

    public List<int> Empty { get; set; } = [];

    public Dictionary<Sku, Sku> Replacements { get; set; } = [];

    // Readable, not writable: JSON Patch can get it, and refuses to remove it.
    public string Reference { get; } = "R-1";

    public string? Note { get; set; }
}

// A type whose JSON form is a number, read by a [JsonConverter] of its own.
[JsonConverter(typeof(MoneyConverter))]
public readonly record struct Money(decimal Amount);

public sealed class MoneyConverter : JsonConverter<Money>
{
    public override Money ReadJson(JsonReader reader, Type objectType, Money existingValue, bool hasExistingValue, JsonSerializer serializer)
        => new(Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture));

    public override void WriteJson(JsonWriter writer, Money value, JsonSerializer serializer) => writer.WriteValue(value.Amount);
}

// A type whose converter is registered in JsonSerializerSettings.Converters only.
public readonly record struct Percent(decimal Value);

public sealed class PercentConverter : JsonConverter<Percent>
{
    public override Percent ReadJson(JsonReader reader, Type objectType, Percent existingValue, bool hasExistingValue, JsonSerializer serializer)
        => new(Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture));

    public override void WriteJson(JsonWriter writer, Percent value, JsonSerializer serializer) => writer.WriteValue(value.Value);
}

// A type whose JSON form is a string, given its converter by the contract resolver; it exposes no member.
public readonly struct Sku : IEquatable<Sku>
{
    private readonly string? _value;

    public Sku(string value) => _value = value;

    public bool Equals(Sku other) => _value == other._value;

    public override bool Equals(object? obj) => obj is Sku other && Equals(other);

    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    public override string ToString() => _value ?? "(default)";
}

public sealed class SkuConverter : JsonConverter<Sku>
{
    public override Sku ReadJson(JsonReader reader, Type objectType, Sku existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var text = (string)reader.Value!;
        return text.Length == 8 ? new Sku(text) : throw new JsonSerializationException("A SKU has exactly 8 characters.");
    }

    public override void WriteJson(JsonWriter writer, Sku value, JsonSerializer serializer) => writer.WriteValue(value.ToString());
}

public sealed class AppContractResolver : DefaultContractResolver
{
    public AppContractResolver() => NamingStrategy = new CamelCaseNamingStrategy();

    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);
        if (objectType == typeof(Sku))
        {
            contract.Converter = new SkuConverter();
        }

        return contract;
    }
}
