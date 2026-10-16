using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");

// A minimal API application: no AddControllers(), no AddNewtonsoftJson(), System.Text.Json options of its own.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument((settings, services) =>
    Console.WriteLine($"AddOpenApiDocument: SchemaSettings is {settings.SchemaSettings.GetType().Name}"));

var app = builder.Build();
app.UseOpenApi();
app.MapGet("/orders/{id}", (int id) => new Order(id, OrderStatus.Shipped));
await app.StartAsync();

using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

Console.WriteLine($"GET /orders/7 -> {await http.GetStringAsync("/orders/7")}");

var document = JsonDocument.Parse(await http.GetStringAsync("/swagger/v1/swagger.json"));
var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
foreach (var name in new[] { "Order", "OrderStatus" })
{
    Console.WriteLine($"components.schemas.{name} -> {JsonSerializer.Serialize(schemas.GetProperty(name))}");
}

Console.WriteLine($"Microsoft.AspNetCore.Mvc.NewtonsoftJson loaded once the document is built: {AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.AspNetCore.Mvc.NewtonsoftJson")}");

await app.StopAsync();

public record Order(int OrderId, OrderStatus Status);

public enum OrderStatus
{
    Pending,
    Shipped,
}
