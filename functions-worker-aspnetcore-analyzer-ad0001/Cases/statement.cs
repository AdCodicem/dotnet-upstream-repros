using Microsoft.Extensions.Hosting;

internal static class Program
{
    // Expected: AZFW0014 on ConfigureFunctionsWorkerDefaults, as in Cases/assigned.cs.
    private static void Main()
    {
        var hostBuilder = new HostBuilder();
        hostBuilder.ConfigureFunctionsWorkerDefaults();
        hostBuilder.Build().Run();
    }
}
