using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();

// Same parameter type and name; only the binding attributes differ.
app.MapGet("/query", (Guid id) => $"query {id}");
app.MapGet("/header", ([FromHeader(Name = "X-Id")] Guid id) => $"header {id}");

// Same source, different header names.
app.MapGet("/header-a", ([FromHeader(Name = "X-A")] string value) => $"X-A {value}");
app.MapGet("/header-b", ([FromHeader(Name = "X-B")] string value) => $"X-B {value}");

await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

var id = Guid.NewGuid();
await Send("/query?id=" + id, null);
await Send("/header", ("X-Id", id.ToString()));
await Send("/header-a", ("X-A", "a"));
await Send("/header-b", ("X-B", "b"));

// What the second endpoint of each pair actually binds.
await Send("/header?id=" + id, null);
await Send("/header-b", ("X-A", "a"));

await app.StopAsync();

async Task Send(string path, (string Name, string Value)? header)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, path);
    if (header is { } h) request.Headers.Add(h.Name, h.Value);
    using var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"GET {path,-48} {(header is { } x ? x.Name + ": " + x.Value : ""),-44} -> {(int)response.StatusCode} {body}");
}
