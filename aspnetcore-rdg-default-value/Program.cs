var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

app.MapGet("/page-1", (int page = 1) => $"page {page}");
app.MapGet("/page-2", (int page = 2) => $"page {page}");

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
Console.WriteLine("GET /page-1 -> " + await client.GetStringAsync("/page-1"));
Console.WriteLine("GET /page-2 -> " + await client.GetStringAsync("/page-2"));
await app.StopAsync();
