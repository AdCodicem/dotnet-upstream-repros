using Microsoft.Extensions.Hosting;

internal static class Program
{
    // The statement of Cases/statement.cs at column 0, without leading trivia: AZFW0014, as designed.
    private static void Main()
    {
        var hostBuilder = new HostBuilder();
hostBuilder.ConfigureFunctionsWorkerDefaults();
        hostBuilder.Build().Run();
    }
}
