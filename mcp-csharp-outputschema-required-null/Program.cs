using System.ComponentModel;
using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// Serializer options that keep nulls, for comparison (and as a workaround).
var keepNulls = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions) { DefaultIgnoreCondition = JsonIgnoreCondition.Never };

if (args is ["--stdio"] or ["--stdio", "--keep-nulls"])
{
    // The same server over stdio, for client/client.mjs (the TypeScript SDK's client).
    var services = new ServiceCollection();
    services.AddMcpServer()
        .WithStreamServerTransport(Console.OpenStandardInput(), Console.OpenStandardOutput())
        .WithTools<OrderTools>(args is [_, "--keep-nulls"] ? keepNulls : null);
    await using var provider = services.BuildServiceProvider();
    await provider.GetRequiredService<McpServer>().RunAsync();
    return;
}

Console.WriteLine($"McpJsonUtilities.DefaultOptions.DefaultIgnoreCondition = {McpJsonUtilities.DefaultOptions.DefaultIgnoreCondition}");
await RunAsync("WithTools<OrderTools>()", b => b.WithTools<OrderTools>());
await RunAsync("WithTools<OrderTools>(keepNulls), keepNulls = DefaultOptions with DefaultIgnoreCondition = Never", b => b.WithTools<OrderTools>(keepNulls));

static async Task RunAsync(string title, Action<IMcpServerBuilder> configure)
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
        Console.WriteLine($"== {title} (negotiated {client.NegotiatedProtocolVersion})");
        foreach (var tool in (await client.ListToolsAsync()).OrderBy(t => t.Name))
        {
            Console.WriteLine($"tools/list  {tool.Name,-12} outputSchema:      {tool.ProtocolTool.OutputSchema?.GetRawText()}");
            var result = await client.CallToolAsync(new CallToolRequestParams { Name = tool.Name });
            Console.WriteLine($"tools/call  {tool.Name,-12} structuredContent: {result.StructuredContent?.GetRawText()}");
        }
    }

    cts.Cancel();
    try { await run; } catch (OperationCanceledException) { }
}

[McpServerToolType]
public sealed class OrderTools
{
    [McpServerTool(Name = "get_order", UseStructuredContent = true), Description("Gets an order.")]
    public static Order GetOrder() => new("42", null);

    [McpServerTool(Name = "get_invoice", UseStructuredContent = true), Description("Gets an invoice.")]
    public static Invoice GetInvoice() => new() { Number = "F-1" };

    [McpServerTool(Name = "get_parcel", UseStructuredContent = true), Description("Gets a parcel.")]
    public static Parcel GetParcel() => new() { Id = "P-7", Label = null };

    [McpServerTool(Name = "get_shipment", UseStructuredContent = true), Description("Gets a shipment.")]
    public static Shipment GetShipment() => new("S-3");
}

public sealed record Order(string Id, string? Note);

public sealed record Shipment(string Id, string? Carrier = null);

public sealed class Parcel
{
    public required string Id { get; init; }

    public required string? Label { get; init; }
}

public sealed class Invoice
{
    public string Number { get; set; } = "";

    public DateOnly? PaidOn { get; set; }
}
