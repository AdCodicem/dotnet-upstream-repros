var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

app.MapGet("/double", (double d = -0.0) => double.IsNegative(d) ? "-0.0" : "+0.0");
app.MapGet("/float", (float f = -0.0f) => float.IsNegative(f) ? "-0.0f" : "+0.0f");
app.MapGet("/decimal", (decimal m = -0.0m) => decimal.IsNegative(m) ? "-0.0m" : "+0.0m");

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
foreach (var path in new[] { "/double", "/float", "/decimal", "/double?d=-0" })
{
    Console.WriteLine($"GET {path} -> " + await client.GetStringAsync(path));
}
await app.StopAsync();
