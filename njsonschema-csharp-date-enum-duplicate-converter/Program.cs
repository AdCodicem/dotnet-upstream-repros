using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

// Generates C# into client/, one generator per process (repro.sh runs both):
//   njsonschema  NJsonSchema's own CSharpGenerator over holiday.schema.json, where the property template lives;
//   nswag        NSwag's client from openapi.json, which holds the same schemas, under Newtonsoft.Json and System.Text.Json.
// The two run apart because NJsonSchema caches parsed templates in a static dictionary keyed by language and template
// name: in one process, whichever runs second gets the other's "File" template and writes an empty namespace.
if (args[0] == "njsonschema")
{
    var holiday = await JsonSchema.FromJsonAsync(await File.ReadAllTextAsync("holiday.schema.json"));
    var njsonCode = new CSharpGenerator(holiday, new CSharpGeneratorSettings { Namespace = "NJsonSchemaOnly" }).GenerateFile();
    await File.WriteAllTextAsync("client/NJsonSchemaOnly.g.cs", njsonCode);
    Print("NJsonSchema CSharpGenerator over holiday.schema.json -> client/NJsonSchemaOnly.g.cs", njsonCode);
    return;
}

var document = await OpenApiDocument.FromFileAsync("openapi.json");
foreach (var (library, file) in new[] { (CSharpJsonLibrary.NewtonsoftJson, "NSwagNewtonsoftJson"), (CSharpJsonLibrary.SystemTextJson, "NSwagSystemTextJson") })
{
    var settings = new CSharpClientGeneratorSettings { ClassName = "HolidaysClient" };
    settings.CSharpGeneratorSettings.Namespace = file;
    settings.CSharpGeneratorSettings.JsonLibrary = library;
    var code = new CSharpClientGenerator(document, settings).GenerateFile();
    await File.WriteAllTextAsync($"client/{file}.g.cs", code);
    Print($"NSwag CSharpClientGenerator over openapi.json, JsonLibrary = {library} -> client/{file}.g.cs", code);
}

// Prints each property of the class Holiday with its attributes.
static void Print(string title, string code)
{
    Console.WriteLine($"== {title}");
    var lines = code.Split('\n').Select(l => l.Trim()).ToArray();
    var attributes = new List<string>();
    foreach (var line in lines.Skip(Array.FindIndex(lines, l => l.EndsWith("partial class Holiday")) + 1))
    {
        if (line.StartsWith('['))
        {
            attributes.Add(line);
        }
        else if (line.StartsWith("public ") && line.EndsWith("{ get; set; }"))
        {
            Console.WriteLine($"  {line}");
            attributes.ForEach(a => Console.WriteLine($"      {a}"));
            attributes.Clear();
        }
        else if (line.Contains("AdditionalProperties"))
        {
            break;
        }
    }
}
