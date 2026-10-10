var app = WebApplication.CreateSlimBuilder(args).Build();

// RouteHandlerAnalyzer throws NullReferenceException on each of these two calls (warning AD0001).
app.MapGet("/creation", new Func<int, string>(Handlers.Size));
app.MapGet("/converted", (Func<int, string>)(size => $"size {size}"));

// The same handler as a method group and as a lambda: no exception.
app.MapGet("/group", Handlers.Size);
app.MapGet("/lambda", (int size) => $"size {size}");

app.Run();

static class Handlers
{
    public static string Size(int size) => $"size {size}";
}
