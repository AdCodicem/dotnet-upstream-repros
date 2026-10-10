using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");

// One document per case, so that a failure in one does not hide the others.
(string Document, Delegate Handler)[] cases =
[
    ("struct-nullable", (NullableStructWithDefault body) => body),
    ("struct", (StructWithDefault body) => body),
    ("struct-nullable-no-default", (NullableStructWithoutDefault body) => body),
    ("int-nullable", (NullableIntWithDefault body) => body),
    ("long", (LongWithIntDefault body) => body),
    ("decimal", (DecimalWithDoubleDefault body) => body),
    ("decimal-typed-default", (DecimalWithTypedDefault body) => body),
    ("date-only", (DateOnlyWithStringDefault body) => body),
    ("long-query-parameter", ([DefaultValue(5)] long count) => count),
    // The (Type, string) form of the attribute converts the text through the type's TypeConverter.
    ("date-only-typed-default", (DateOnlyWithTypedDefault body) => body),
    ("struct-typed-default", (StructWithTypedDefault body) => body),
    ("struct-type-converter-typed-default", (ConvertibleStructWithTypedDefault body) => body),
];
foreach (var (document, _) in cases)
{
    builder.Services.AddOpenApi(document);
}

var app = builder.Build();
Exception? failure = null;
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception e)
    {
        failure = e;
        context.Response.StatusCode = 500;
    }
});
app.MapOpenApi();
foreach (var (document, handler) in cases)
{
    app.MapPost("/" + document, handler).WithGroupName(document);
}

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
var printedTrace = false;
foreach (var (document, _) in cases)
{
    failure = null;
    using var response = await client.GetAsync($"/openapi/{document}.json");
    Console.WriteLine($"== GET /openapi/{document}.json -> {(int)response.StatusCode}");
    if (response.IsSuccessStatusCode)
    {
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        foreach (var (name, schema) in json["components"]?["schemas"]?.AsObject() ?? [])
        {
            Console.WriteLine($"   {name}: {schema!.ToJsonString()}");
        }
    }
    else if (failure is not null)
    {
        if (printedTrace)
        {
            // The exception and the two frames that say where the default value was being written.
            Console.WriteLine($"   {failure.GetType()}: {failure.Message}");
            foreach (var frame in failure.StackTrace!.Split('\n').Where(f => f.Contains("Microsoft.AspNetCore.OpenApi.")).Take(2))
            {
                Console.WriteLine($"   {frame.Trim()}");
            }
        }
        else
        {
            Console.WriteLine(failure);
            printedTrace = true;
        }
    }
}
await app.StopAsync();

// A struct that travels as a JSON string through its own converter, as a strongly typed value does.
[JsonConverter(typeof(CountryCodeJsonConverter))]
public readonly struct CountryCode(string value)
{
    public string Value { get; } = value;
}

public sealed class CountryCodeJsonConverter : JsonConverter<CountryCode>
{
    public override CountryCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, CountryCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

// The same, with a TypeConverter from its text as well.
[JsonConverter(typeof(CurrencyCodeJsonConverter))]
[TypeConverter(typeof(CurrencyCodeTypeConverter))]
public readonly struct CurrencyCode(string value)
{
    public string Value { get; } = value;
}

public sealed class CurrencyCodeJsonConverter : JsonConverter<CurrencyCode>
{
    public override CurrencyCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, CurrencyCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public sealed class CurrencyCodeTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => new CurrencyCode((string)value);
}

public class NullableStructWithDefault
{
    [DefaultValue("FR")]
    public CountryCode? Country { get; set; }
}

public class StructWithDefault
{
    [DefaultValue("FR")]
    public CountryCode Country { get; set; }
}

public class NullableStructWithoutDefault
{
    public CountryCode? Country { get; set; }
}

public class NullableIntWithDefault
{
    [DefaultValue(5)]
    public int? Count { get; set; }
}

public class LongWithIntDefault
{
    [DefaultValue(5)]
    public long Count { get; set; }
}

public class DecimalWithDoubleDefault
{
    [DefaultValue(0.5)]
    public decimal Rate { get; set; }
}

public class DecimalWithTypedDefault
{
    [DefaultValue(typeof(decimal), "0.5")]
    public decimal Rate { get; set; }
}

public class DateOnlyWithStringDefault
{
    [DefaultValue("2026-01-01")]
    public DateOnly Day { get; set; }
}

public class DateOnlyWithTypedDefault
{
    [DefaultValue(typeof(DateOnly), "2026-01-01")]
    public DateOnly Day { get; set; }
}

public class StructWithTypedDefault
{
    [DefaultValue(typeof(CountryCode), "FR")]
    public CountryCode Country { get; set; }
}

public class ConvertibleStructWithTypedDefault
{
    [DefaultValue(typeof(CurrencyCode), "EUR")]
    public CurrencyCode Currency { get; set; }
}
