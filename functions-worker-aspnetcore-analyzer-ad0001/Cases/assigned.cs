using Microsoft.Extensions.Hosting;

internal static class Program
{
    // The same call, its result assigned: AZFW0014, as designed.
    private static void Main()
    {
        var hostBuilder = new HostBuilder();
        var configured = hostBuilder.ConfigureFunctionsWorkerDefaults();
        configured.Build().Run();
    }
}
