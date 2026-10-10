var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

app.MapGet("/{*path}", () => "catch-all");
app.MapFallback(() => "fallback");

await app.StartAsync();
foreach (var endpoint in ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>())
{
    // FallbackMetadata is internal to Microsoft.AspNetCore.Routing, hence the lookup by name.
    var fallbackMetadata = endpoint.Metadata.Any(metadata => metadata.GetType().Name == "FallbackMetadata");
    Console.WriteLine($"{endpoint.RoutePattern.RawText}: Order {endpoint.Order}, DisplayName \"{endpoint.DisplayName}\", FallbackMetadata {fallbackMetadata}");
}
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
Console.WriteLine("GET /missing -> " + await client.GetStringAsync("/missing"));
await app.StopAsync();
