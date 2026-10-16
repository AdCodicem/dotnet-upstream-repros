// HotChocolate's cost analysis weighs StringOperationFilterInputType's contains/startsWith/endsWith (and their
// negations) as expensive operations, DefaultExpensiveFilterOperationCost (20), through `.Expensive()`, a `file`-scoped
// extension that sets the internal CoreFieldFlags.FilterExpensiveOperationField. A filter operation type of the
// application's own has no public way to set that flag, so the very same operation, handled by the very same handler,
// is weighed as a cheap one, DefaultFilterOperationCost (10). Writing @cost by hand works, but ignores the
// application's FilterCostOptions.
using HotChocolate;
using HotChocolate.CostAnalysis;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

await Show("1. HotChocolate's own StringOperationFilterInputType",
    builder => builder.AddFiltering());
await Show("2. An operation type of the application's own, deriving StringOperationFilterInputType (same handlers)",
    builder => builder.AddFiltering(c => c.AddDefaults().BindRuntimeType<string, AppStringOperationFilterInputType>()));
await Show("3. HotChocolate's own, with FilterCostOptions.DefaultExpensiveFilterOperationCost = 50",
    builder => builder.AddFiltering()
        .ModifyCostOptions(o => o.Filtering.DefaultExpensiveFilterOperationCost = 50));
await Show("4. The workaround, @cost(weight: 20) written by hand, with DefaultExpensiveFilterOperationCost = 50",
    builder => builder
        .AddFiltering(c => c.AddDefaults().BindRuntimeType<string, HandWeighedStringOperationFilterInputType>())
        .ModifyCostOptions(o => o.Filtering.DefaultExpensiveFilterOperationCost = 50));

static async Task Show(string title, Action<IRequestExecutorBuilder> configure)
{
    Console.WriteLine($"== {title}");
    var services = new ServiceCollection();
    var builder = services.AddGraphQL().AddQueryType<Query>().AddCostAnalyzer();
    configure(builder);
    var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();

    var sdl = executor.Schema.ToString();
    var inBlock = false;
    foreach (var line in sdl.Split('\n'))
    {
        if (line.StartsWith("input ") && line.Contains("StringOperationFilterInput"))
        {
            inBlock = true;
        }
        if (inBlock && (line.StartsWith("input ") || line.Contains("contains:") || line.Contains("startsWith:") || line.Contains("eq:")))
        {
            Console.WriteLine($"   {line.TrimEnd()}");
        }
        if (inBlock && line.StartsWith("}"))
        {
            inBlock = false;
        }
    }

    var request = OperationRequestBuilder.New()
        .SetDocument("{ books(where: { title: { contains: \"a\" } }) { title } }")
        .ReportCost()
        .Build();
    var result = await executor.ExecuteAsync(request);
    var json = System.Text.Json.Nodes.JsonNode.Parse(result.ToJson())!;
    Console.WriteLine($"   {{ books(where: {{ title: {{ contains: \"a\" }} }}) {{ title }} }} -> extensions.operationCost = {json["extensions"]?["operationCost"]?.ToJsonString()}");
    Console.WriteLine();
}

public class Book
{
    public string Title { get; set; } = "";
}

public class Query
{
    [UseFiltering]
    public IQueryable<Book> Books() => new[] { new Book { Title = "a" }, new Book { Title = "b" } }.AsQueryable();
}

// Same handlers as HotChocolate's (they match `context.Type is StringOperationFilterInputType`); only the name and the
// set of operations are the application's.
public class AppStringOperationFilterInputType : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("AppStringOperationFilterInput");
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
        // descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>().Expensive();  // not public
    }
}

public class HandWeighedStringOperationFilterInputType : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("HandWeighedStringOperationFilterInput");
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>().Directive(new CostDirective(20));
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>().Directive(new CostDirective(20));
    }
}
