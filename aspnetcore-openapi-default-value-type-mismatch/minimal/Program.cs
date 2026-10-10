using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapPost("/orders", (Order order) => order);

await app.StartAsync();
using (var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) })
{
    using var response = await client.GetAsync("/openapi/v1.json");
    Console.WriteLine($"GET /openapi/v1.json -> {(int)response.StatusCode}");
}
await app.StopAsync();
await app.DisposeAsync(); // flushes the console logger

public class Order
{
    [DefaultValue(5)]
    public long Quantity { get; set; }
}
