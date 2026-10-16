using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;

var options = typeof(OptionsValidatorAttribute).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
Console.WriteLine($"Microsoft.Extensions.Options {options.Split('+')[0]} (its [OptionsValidator] generator), runtime {Environment.Version}");

var invalid = new ServerOptions { Host = null, Port = 0, Tls = new TlsOptions { CertificatePath = null } };
foreach (var name in new[] { Options.DefaultName, "Backup" })
{
    Console.WriteLine($"-- [OptionsValidator], options name \"{name}\":");
    foreach (var failure in new ServerOptionsValidator().Validate(name, invalid).Failures!)
    {
        Console.WriteLine($"   {failure}");
    }
}
Console.WriteLine("-- ValidateDataAnnotations() (reflection), either name:");
foreach (var failure in new DataAnnotationValidateOptions<ServerOptions>("Backup").Validate("Backup", invalid).Failures!)
{
    Console.WriteLine($"   {failure}");
}

public sealed class ServerOptions
{
    [Required]
    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; }

    [ValidateObjectMembers]
    public TlsOptions? Tls { get; set; }
}

public sealed class TlsOptions
{
    [Required]
    public string? CertificatePath { get; set; }
}

[OptionsValidator]
public sealed partial class ServerOptionsValidator : IValidateOptions<ServerOptions>;
