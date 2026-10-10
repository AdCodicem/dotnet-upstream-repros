using System.ComponentModel;
using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// Two registrations of the same method: the SDK's own tool, and the same tool behind a DelegatingMcpServerTool
// that overrides nothing.
static void AddTools(IMcpServerBuilder builder, bool withWrapped)
{
    builder.WithTools<CountTools>();
    if (withWrapped)
    {
        var method = typeof(CountTools).GetMethod(nameof(CountTools.Count))!;
        builder.WithTools([new PassThroughTool(McpServerTool.Create(method, options: new() { Name = "count_wrapped" }))]);
    }
}

if (args is ["--stdio", ..])
{
    // The same server over stdio, for client/client.mjs (the TypeScript SDK's client).
    var services = new ServiceCollection();
    AddTools(
        services.AddMcpServer().WithStreamServerTransport(Console.OpenStandardInput(), Console.OpenStandardOutput()),
        withWrapped: args is not [_, "--sdk-tool-only"]);
    await using var provider = services.BuildServiceProvider();
    await provider.GetRequiredService<McpServer>().RunAsync();
    return;
}

foreach (var protocolVersion in new[] { null, "2025-11-25", "2025-06-18" })
{
    Pipe clientToServer = new(), serverToClient = new();
    var services = new ServiceCollection();
    AddTools(
        services.AddMcpServer().WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()),
        withWrapped: true);
    await using var provider = services.BuildServiceProvider();
    using var cts = new CancellationTokenSource();
    var run = provider.GetRequiredService<McpServer>().RunAsync(cts.Token);

    await using (var client = await McpClient.CreateAsync(
        new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
        new McpClientOptions { ProtocolVersion = protocolVersion }))
    {
        Console.WriteLine($"== McpClientOptions.ProtocolVersion = {protocolVersion ?? "(not set)"}, negotiated {client.NegotiatedProtocolVersion}");
        foreach (var tool in (await client.ListToolsAsync()).OrderBy(t => t.Name))
        {
            Console.WriteLine($"tools/list  {tool.Name,-13} outputSchema:      {tool.ProtocolTool.OutputSchema?.GetRawText()}");
            var result = await client.CallToolAsync(new CallToolRequestParams { Name = tool.Name });
            Console.WriteLine($"tools/call  {tool.Name,-13} structuredContent: {result.StructuredContent?.GetRawText()}");
        }
    }

    cts.Cancel();
    try { await run; } catch (OperationCanceledException) { }
}

[McpServerToolType]
public sealed class CountTools
{
    [McpServerTool(Name = "count", UseStructuredContent = true), Description("Counts the open orders.")]
    public static int Count() => 72;
}

/// <summary>A tool chained around the SDK's tool, as DelegatingMcpServerTool's documentation recommends. It overrides nothing.</summary>
public sealed class PassThroughTool(McpServerTool innerTool) : DelegatingMcpServerTool(innerTool);
