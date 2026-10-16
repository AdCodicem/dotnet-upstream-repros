# Microsoft.Gen.Logging: a classification on a class or struct is not read when a value of that type is logged

`DataClassificationAttribute` may be applied to a class or a struct (its `AttributeUsage` includes `Class`, `Struct` and
`Interface`). The `[LoggerMessage]` generator of Microsoft.Extensions.Telemetry.Abstractions reads a classification on
the parameter only: a parameter whose type is classified goes to the plain `TagArray` and is logged in clear, whatever
redactor is configured. `LOGGEN035` misses the same case in a record logged whole, while it reports a record whose
property, or the record type of whose property, is classified.

```bash
./repro.sh    # .NET 10, Microsoft.Extensions.Telemetry.Abstractions 10.11.0; prints the generated tag writes, then the tags a logger provider receives
```

- `runtime/`: a console app with redaction enabled and a fallback redactor that writes `<redacted>`.
- `record-check/`: a library that builds with `LOGGEN035` (and the `CS8795` that follows) for two records, and with
  nothing for the third.

Wanted: case 2 redacted like case 1, and `LOGGEN035` for `Contact`. See `repro-output-net10.0.txt`.
