var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

app.MapFallback("/api/{*path}", () => "api fallback");
app.MapFallback(() => "site fallback");

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
Console.WriteLine("GET /api/missing -> " + await client.GetStringAsync("/api/missing"));
Console.WriteLine("GET /missing -> " + await client.GetStringAsync("/missing"));
await app.StopAsync();
