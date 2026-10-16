using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

Console.WriteLine($"Swashbuckle.AspNetCore.SwaggerGen {typeof(ISchemaFilter).Assembly.GetName().Version}");

Console.WriteLine("A schema filter registered with c.SchemaFilter<T>(), whose constructor takes:");
await Run("nothing", c => c.SchemaFilter<NoDependencyFilter>());
await Run("IOptions<SchemaGeneratorOptions>", c => c.SchemaFilter<OptionsAccessorFilter>());
await Run("SchemaGeneratorOptions", c => c.SchemaFilter<SchemaGeneratorOptionsFilter>());
await Run("ISchemaGenerator", c => c.SchemaFilter<SchemaGeneratorFilter>());
await Run("ISerializerDataContractResolver", c => c.SchemaFilter<DataContractResolverFilter>());

Console.WriteLine("An operation filter registered with c.OperationFilter<T>(), whose constructor takes:");
await Run("ISchemaGenerator", c => c.OperationFilter<OperationFilterTaking<ISchemaGenerator>>());
await Run("SwaggerGeneratorOptions", c => c.OperationFilter<OperationFilterTaking<SwaggerGeneratorOptions>>());
await Run("ISwaggerProvider", c => c.OperationFilter<OperationFilterTaking<ISwaggerProvider>>());

Console.WriteLine("A document filter registered with c.DocumentFilter<T>(), whose constructor takes:");
await Run("ISchemaGenerator", c => c.DocumentFilter<DocumentFilterTaking<ISchemaGenerator>>());
await Run("SwaggerGeneratorOptions", c => c.DocumentFilter<DocumentFilterTaking<SwaggerGeneratorOptions>>());
await Run("ISwaggerProvider", c => c.DocumentFilter<DocumentFilterTaking<ISwaggerProvider>>());

// The threads of a request that never answered are still blocked; nothing can release them.
Environment.Exit(0);

static async Task Run(string dependency, Action<SwaggerGenOptions> addFilter)
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(addFilter);

    var app = builder.Build();
    app.UseSwagger();
    app.MapGet("/todo", () => new Todo(1, "Write the repro"));
    await app.StartAsync();

    using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()), Timeout = TimeSpan.FromSeconds(10) };
    try
    {
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"  {dependency,-34} GET /swagger/v1/swagger.json -> {(int)response.StatusCode}, {body.Length} chars");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine($"  {dependency,-34} GET /swagger/v1/swagger.json -> no response after 10 s");
    }

    using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(1));
    try { await app.StopAsync(stopping.Token); } catch (OperationCanceledException) { }
}

record Todo(int Id, string Title);

sealed class NoDependencyFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context) { }
}

sealed class OptionsAccessorFilter(IOptions<SchemaGeneratorOptions> options) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context) => _ = options;
}

sealed class SchemaGeneratorOptionsFilter(SchemaGeneratorOptions options) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context) => _ = options;
}

sealed class SchemaGeneratorFilter(ISchemaGenerator generator) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context) => _ = generator;
}

sealed class DataContractResolverFilter(ISerializerDataContractResolver resolver) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context) => _ = resolver;
}

sealed class OperationFilterTaking<TDependency>(TDependency dependency) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context) => _ = dependency;
}

sealed class DocumentFilterTaking<TDependency>(TDependency dependency) : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context) => _ = dependency;
}
