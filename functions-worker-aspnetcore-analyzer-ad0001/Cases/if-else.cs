using Microsoft.Extensions.Hosting;

// Registers the ASP.NET Core integration in one branch and the worker defaults in the other, as the Functions host's
// own test application does (#2888). Expected: no diagnostic.
var useAspNetCore = args.Contains("--aspnetcore");
var hostBuilder = new HostBuilder();
if (useAspNetCore)
{
    hostBuilder.ConfigureFunctionsWebApplication();
}
else
{
    hostBuilder.ConfigureFunctionsWorkerDefaults();
}
hostBuilder.Build().Run();
