# Microsoft.Gen.Logging: `[LogProperties]` expansion drops a property's classification and, under `Transitive`, its `[LogPropertyIgnore]`

When the `[LoggerMessage]` generator expands a property into its own properties, whether because the parameter's
`[LogProperties]` is `Transitive` or because the property carries `[LogProperties]` itself, it drops the property's
data classification and writes its sub-properties to the plain `TagArray`, in clear. Under `Transitive = true`, a
`[LogPropertyIgnore]` property is logged whole. `Transitive` on a property's own `[LogProperties]` is not read.

```bash
./repro.sh    # .NET 10, Microsoft.Extensions.Telemetry.Abstractions 10.11.0; prints the generated tag writes, then the tags a logger provider receives
```

Wanted: cases B and C redacted like case A (or a diagnostic, as `LOGGEN017` reports for a parameter), case E without
`account.Password`, like case D, and case F expanding `Geo`. See `repro-output-net10.0.txt`.
