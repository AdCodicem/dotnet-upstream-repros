using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddRazorComponents();
var app = builder.Build();
app.UseAntiforgery();
app.MapRazorComponents<Repro.App>();
await app.StartAsync();

var endpoints = typeof(RazorComponentsEndpointRouteBuilderExtensions).Assembly;
Console.WriteLine($"{RuntimeInformation.FrameworkDescription}, {endpoints.GetName().Name} {endpoints.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion}");

// Posts the static form as a browser would: the antiforgery token and cookie come from a GET of the page.
using var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(app.Urls.First()) };
var page = await client.GetStringAsync("/");
var token = Regex.Match(page, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

async Task PostAsync(string title, string code, string number, string id, string tag)
{
    var fields = new Dictionary<string, string>
    {
        ["_handler"] = "repro",
        ["__RequestVerificationToken"] = token,
        ["Model.Code"] = code,
        ["Model.Number"] = number,
        ["Model.Id"] = id,
        ["Model.Tag"] = tag,
    };
    using var response = await client.PostAsync("/", new FormUrlEncodedContent(fields));
    var html = await response.Content.ReadAsStringAsync();
    Console.WriteLine();
    Console.WriteLine($"POST / ({title}) -> {(int)response.StatusCode}");
    Console.WriteLine(WebUtility.HtmlDecode(Regex.Match(html, "<pre id=\"result\">([^<]*)</pre>").Groups[1].Value).TrimEnd());
}

// A browser posts an input left empty as an empty value: Model.Code=&Model.Number=&Model.Id=&Model.Tag=
await PostAsync("every input left empty", "", "", "", "");
await PostAsync("every input filled", "ABC", "7", "6f9619ff-8b86-d011-b42d-00c04fc964ff", "x");
await app.StopAsync();
