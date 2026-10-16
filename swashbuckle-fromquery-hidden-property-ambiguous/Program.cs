using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

Console.WriteLine($"Swashbuckle.AspNetCore.SwaggerGen {typeof(ISchemaFilter).Assembly.GetName().Version}");

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.MapControllers();
await app.StartAsync();

// MVC binds the model: the derived int property receives the value.
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()), Timeout = TimeSpan.FromSeconds(10) };
Console.WriteLine("GET /search?code=42 -> " + await client.GetStringAsync("/search?code=42"));

using var response = await client.GetAsync("/swagger/v1/swagger.json");
Console.WriteLine($"GET /swagger/v1/swagger.json -> {(int)response.StatusCode}");
if (response.IsSuccessStatusCode)
{
    var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    Console.WriteLine("parameters of GET /search: " + document["paths"]!["/search"]!["get"]!["parameters"]!.ToJsonString());
}
else
{
    // The same document, generated directly, to show the exception.
    try
    {
        app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
    }
    catch (Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            Console.WriteLine($"{current.GetType().FullName}: {current.Message}");
            foreach (var frame in current.StackTrace!.Split(Environment.NewLine).Take(10))
            {
                Console.WriteLine(frame.Split(" in ")[0]);
            }
        }
    }
}

await app.StopAsync();

public class BaseQuery
{
    public string? Code { get; set; }
}

public class DerivedQuery : BaseQuery
{
    // Hides the base property with another type.
    public new int Code { get; set; }
}

[ApiController]
public class SearchController : ControllerBase
{
    [HttpGet("/search")]
    public string Search([FromQuery] DerivedQuery query) => $"Code = {query.Code}";
}
