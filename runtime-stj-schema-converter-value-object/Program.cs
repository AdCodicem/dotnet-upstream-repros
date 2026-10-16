using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

var options = JsonSerializerOptions.Web;
var order = new Order(new Iban("FR7630006000011234567890189"), null, [new Iban("DE89370400440532013000")], new() { ["main"] = new Iban("GB29NWBK60161331926819") });
Console.WriteLine($"-- the wire: {JsonSerializer.Serialize(order, options)}");

var calls = new List<string>();
var schema = JsonSchemaExporter.GetJsonSchemaAsNode(options, typeof(Order), new JsonSchemaExporterOptions
{
    TransformSchemaNode = (context, node) =>
    {
        calls.Add($"   /{string.Join("/", context.Path.ToArray())}: {Name(context.TypeInfo.Type)} ({context.TypeInfo.Kind}) {node.ToJsonString()}");
        return node;
    },
});
Console.WriteLine($"-- the schema: {schema.ToJsonString()}");
Console.WriteLine("-- the nodes TransformSchemaNode is called for:");
calls.ForEach(Console.WriteLine);

static string Name(Type type) => Nullable.GetUnderlyingType(type) is { } underlying ? $"{underlying.Name}?" : type.Name;

public sealed record Order(Iban Account, Iban? Fallback, List<Iban> History, Dictionary<string, Iban> ByName);

// A single-value struct a converter writes as a JSON string, as a source generator would write it.
[JsonConverter(typeof(IbanConverter))]
public readonly record struct Iban(string Value);

public sealed class IbanConverter : JsonConverter<Iban>
{
    public override Iban Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, Iban value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
