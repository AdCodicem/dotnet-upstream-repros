using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NSwag.Generation;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument();

var app = builder.Build();
app.MapControllers();
await app.StartAsync();

// 1. MVC binds both query models.
using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
foreach (var url in new[] { "/same-type?code=FR", "/other-type?code=42" })
{
    Console.WriteLine($"GET {url} -> {(int)(await http.GetAsync(url)).StatusCode} {await http.GetStringAsync(url)}");
}

// 2. NSwag's document.
Console.WriteLine();
try
{
    var document = await app.Services.GetRequiredService<IOpenApiDocumentGenerator>().GenerateAsync("v1");
    foreach (var (path, item) in document.Paths)
    {
        foreach (var parameter in item.Values.SelectMany(o => o.Parameters))
        {
            Console.WriteLine($"{path}: parameter {parameter.Name}, {parameter.Kind}, {parameter.Schema?.Type}");
        }
    }
}
catch (Exception e)
{
    Console.WriteLine($"IOpenApiDocumentGenerator.GenerateAsync(\"v1\") threw {e.GetType().FullName}: {e.Message}");
    foreach (var line in e.StackTrace!.Split('\n').Select(l => l.TrimEnd()).Take(6))
    {
        Console.WriteLine(line);
    }
}

// 3. The reflection call NSwag makes, by itself.
Console.WriteLine();
foreach (var type in new[] { typeof(SameTypeQuery), typeof(OtherTypeQuery) })
{
    try
    {
        var property = type.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance);
        Console.WriteLine($"typeof({type.Name}).GetProperty(\"Code\", Public | Instance) -> {property!.PropertyType.Name} {property.DeclaringType!.Name}.{property.Name}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"typeof({type.Name}).GetProperty(\"Code\", Public | Instance) -> {e.GetType().FullName}: {e.Message}");
    }
}

await app.StopAsync();

public class BaseQuery
{
    public string? Code { get; set; }
}

// Hides the inherited property with one of the same type.
public class SameTypeQuery : BaseQuery
{
    public new string? Code { get; set; }
}

// Hides the inherited property with one of another type.
public class OtherTypeQuery : BaseQuery
{
    public new int Code { get; set; }
}

[ApiController]
public class QueryController : ControllerBase
{
    [HttpGet("same-type")]
    public string SameType([FromQuery] SameTypeQuery query) => $"Code = {query.Code}";

    [HttpGet("other-type")]
    public string OtherType([FromQuery] OtherTypeQuery query) => $"Code = {query.Code}";
}
