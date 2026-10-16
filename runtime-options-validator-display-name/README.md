# `[OptionsValidator]`: the display name of a member drops the options type's name in 11.0, but keeps a named instance's name

The validator `[OptionsValidator]` generates hands each validation attribute a display name. With the generator of
Microsoft.Extensions.Options 10.0 that name is `ServerOptions.Host` for the default options instance and `Backup.Host`
for the instance named `Backup`; with 11.0.0-rc.1 and rc.2 it is `Host` for the default instance, but still
`Backup.Host` for the named one. A nested member reads `Tls.CertificatePath`, where the reflection-based
`ValidateDataAnnotations()` says `CertificatePath`. The change follows the package, not the target framework: the
11.0 packages give the new names on `net10.0` too.

```bash
./repro.sh                      # net10.0: package 10.0.13, 11.0.0-rc.1, 11.0.0-rc.2
REPRO_TFM=net11.0 ./repro.sh    # net11.0 on the 11.0 RC1 SDK
```

See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
