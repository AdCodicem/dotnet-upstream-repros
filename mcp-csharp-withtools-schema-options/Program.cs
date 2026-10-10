using System.ComponentModel;
using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// The rules of a type serialized by a custom JsonConverter, which the generated schema cannot see (it reads `true`).
var rules = new AIJsonSchemaCreateOptions
{
    TransformSchemaNode = (context, node) => context.TypeInfo.Type == typeof(CountryCode)
        ? new JsonObject { ["type"] = "string", ["pattern"] = "^[A-Z]{2}$" }
        : node,
};

await ShowAsync("WithTools<GeoTools>()", b => b.WithTools<GeoTools>());

// The only way to pass SchemaCreateOptions today: build each tool by hand, re-doing what WithTools<T>() does
// (method discovery, and a target created per call for instance methods).
await ShowAsync("McpServerTool.Create(method, options: new() { SchemaCreateOptions = rules }) + WithTools(tools)", b => b.WithTools(
    typeof(GeoTools).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
        .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
        .Select(m => McpServerTool.Create(m, options: new() { SchemaCreateOptions = rules }))));

static async Task ShowAsync(string title, Action<IMcpServerBuilder> configure)
{
    Pipe clientToServer = new(), serverToClient = new();
    var services = new ServiceCollection();
    configure(services.AddMcpServer().WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()));
    await using var provider = services.BuildServiceProvider();
    using var cts = new CancellationTokenSource();
    var run = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);

    await using (var client = await McpClient.CreateAsync(
        new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream())))
    {
        Console.WriteLine($"== {title}");
        foreach (var tool in await client.ListToolsAsync())
        {
            Console.WriteLine($"{tool.Name} inputSchema:  {tool.ProtocolTool.InputSchema.GetRawText()}");
            Console.WriteLine($"{tool.Name} outputSchema: {tool.ProtocolTool.OutputSchema?.GetRawText()}");
        }
    }

    cts.Cancel();
    try { await run; } catch (OperationCanceledException) { }
}

[McpServerToolType]
public sealed class GeoTools
{
    [McpServerTool(Name = "find_office", UseStructuredContent = true), Description("Finds our office in a country.")]
    public static Office FindOffice(CountryCode country) => new(country, "Lyon");
}

public sealed record Office(CountryCode Country, string City);

/// <summary>An ISO 3166-1 alpha-2 code, written to JSON as a bare string by its own converter.</summary>
[JsonConverter(typeof(CountryCodeConverter))]
public readonly record struct CountryCode(string Value);

public sealed class CountryCodeConverter : JsonConverter<CountryCode>
{
    public override CountryCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString() is { Length: 2 } text && text.All(char.IsAsciiLetterUpper)
            ? new CountryCode(text)
            : throw new JsonException("Expected two capital letters.");

    public override void Write(Utf8JsonWriter writer, CountryCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
