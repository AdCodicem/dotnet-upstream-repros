using NJsonSchema;

// JsonSchema.Validate on JSON integers around the Int64 limit, under schemas of every kind.
string[] schemas =
[
    """{}""",
    """{ "type": "integer" }""",
    """{ "type": "number" }""",
    """{ "type": "string" }""",
    """{ "type": "integer", "minimum": 0, "maximum": 18446744073709551615 }""",
];

string[] values =
[
    "9223372036854775807",   // long.MaxValue
    "9223372036854775808",   // long.MaxValue + 1, a ulong
    "18446744073709551615",  // ulong.MaxValue
    "18446744073709551616",  // ulong.MaxValue + 1, a UInt128 or a BigInteger
    "1e20",                  // the same magnitude, written as a real
];

foreach (var schemaJson in schemas)
{
    var schema = await JsonSchema.FromJsonAsync(schemaJson);
    Console.WriteLine($"schema {schemaJson}");
    foreach (var value in values)
    {
        Console.WriteLine($"  {value,-22} -> {Validate(schema, value)}");
    }
}

// The same value inside an object, as a message carries it.
var message = await JsonSchema.FromJsonAsync("""{ "type": "object", "properties": { "id": { "type": "integer" } } }""");
Console.WriteLine("""schema { "type": "object", "properties": { "id": { "type": "integer" } } }""");
Console.WriteLine($"""  {"{\"id\":18446744073709551615}",-22} -> {Validate(message, "{\"id\":18446744073709551615}")}""");

static string Validate(JsonSchema schema, string json)
{
    try
    {
        var errors = schema.Validate(json);
        return errors.Count == 0 ? "valid" : string.Join(", ", errors.Select(e => e.Kind));
    }
    catch (Exception e)
    {
        return $"{e.GetType().FullName}: {e.Message}";
    }
}

// Where it is thrown.
try
{
    (await JsonSchema.FromJsonAsync("{}")).Validate("18446744073709551615");
}
catch (InvalidCastException e)
{
    Console.WriteLine();
    Console.WriteLine("stack trace of the first case:");
    foreach (var line in e.StackTrace!.Split('\n').Select(l => l.TrimEnd()).Take(8))
    {
        Console.WriteLine(line);
    }
}
