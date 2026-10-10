var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

// Three endpoints, none sharing anything with another. Each Map call is tried on its own.
Try(() => app.MapGet("/params", (params int[] ids) => $"params {ids.Length}"));
Try(() => app.MapGet("/func", (Func<int, string>)Handlers.Page));
Try(() => app.MapGet("/named", (SizeHandler)Handlers.Size));

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
foreach (var url in new[] { "/params?ids=1&ids=2", "/func", "/named?size=5" })
{
    using var response = await client.GetAsync(url);
    Console.WriteLine($"GET {url} -> {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
}
await app.StopAsync();

static void Try(Action map)
{
    try { map(); }
    catch (InvalidCastException e) { Console.WriteLine(e); }
}

delegate string SizeHandler(int size);

static class Handlers
{
    public static string Page(int page = 1) => $"page {page}";
    public static string Size(int size) => $"size {size}";
}
