using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

static string Show(string? value) => value is null ? "null" : $"\"{value}\"";

// A string with a default value, bound through an explicit attribute.
app.MapGet("/query", ([FromQuery] string fromQuery = "asc") => Show(fromQuery));
app.MapGet("/header", ([FromHeader] string fromHeader = "asc") => Show(fromHeader));
app.MapGet("/route/{fromRoute?}", ([FromRoute] string fromRoute = "asc") => Show(fromRoute));
// Controls: the same string without an attribute, and an int under the same attribute.
app.MapGet("/inferred", (string inferred = "asc") => Show(inferred));
app.MapGet("/query-int", ([FromQuery] int page = 1) => page.ToString());

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
foreach (var path in new[] { "/query", "/header", "/route", "/inferred", "/query-int", "/query?fromQuery=desc" })
{
    Console.WriteLine($"GET {path} -> {await client.GetStringAsync(path)}");
}
await app.StopAsync();
