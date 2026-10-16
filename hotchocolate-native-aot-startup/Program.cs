// The smallest HotChocolate application: one query type, written for HotChocolate's source generator ([QueryType],
// AddTypes()), its executor built in process, one request. Under the JIT it answers {"data":{"hello":"world"}}; published
// with PublishAot=true it aborts while building the schema, unless HotChocolate.Types and the application's assembly
// are rooted (dotnet publish -p:ReproRoot=true).
using HotChocolate;
using Repro;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine(Runtime.IsNative() ? "== native AOT binary" : "== JIT");
try
{
    var services = new ServiceCollection();
    services.AddGraphQL().AddReproTypes();
    var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();
    var result = await executor.ExecuteAsync("{ hello }");
    Console.WriteLine(result.ToJson().ReplaceLineEndings(" "));
}
catch (Exception exception)
{
    Console.WriteLine($"!! {exception.GetType().FullName}: {exception.Message.Trim().ReplaceLineEndings(" ")}");
    for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
    {
        Console.WriteLine($"   inner {inner.GetType().FullName}: {inner.Message.Trim().ReplaceLineEndings(" ")}");
    }
    Console.WriteLine(exception.StackTrace);
    return 1;
}
return 0;

namespace Repro
{
[QueryType]
public static partial class Query
{
    public static string Hello() => "world";
}
}

internal static class Runtime
{
    // The core library has no file location in a native AOT binary.
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "The empty location is what is tested.")]
    public static bool IsNative() => string.IsNullOrEmpty(typeof(object).Assembly.Location);
}
