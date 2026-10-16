var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

static string Show(object? value) => value is null ? "null" : $"\"{value}\"";

// Fail with the generator on: a constructor parameter of a nullable reference type.
app.MapGet("/record", ([AsParameters] SearchRecord q) => $"{q.Page} {Show(q.Sort)}");
app.MapGet("/class", ([AsParameters] LinkClass q) => Show(q.Next));
// Compile: the same parameter not nullable, a record struct, a class with settable properties.
app.MapGet("/not-nullable", ([AsParameters] SortRecord q) => Show(q.Sort));
app.MapGet("/record-struct", ([AsParameters] SortStruct q) => Show(q.Sort));
app.MapGet("/properties", ([AsParameters] SortProperties q) => Show(q.Sort));

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
foreach (var path in new[] { "/record?page=2", "/record?page=2&sort=name", "/class?next=https://example.org/", "/not-nullable?sort=name", "/record-struct", "/properties" })
{
    Console.WriteLine($"GET {path} -> {await client.GetStringAsync(path)}");
}
await app.StopAsync();

record SearchRecord(int Page, string? Sort);
class LinkClass(Uri? next) { public Uri? Next { get; } = next; }
record SortRecord(string Sort);
record struct SortStruct(string? Sort);
class SortProperties { public string? Sort { get; set; } }
