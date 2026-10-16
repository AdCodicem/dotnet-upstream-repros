# Microsoft.Gen.Logging: a classification attribute's constructor arguments are dropped

The `[LoggerMessage]` generator builds each classification as `new TAttribute().Classification`, whatever arguments
the attribute was written with. A classification attribute whose constructor requires an argument is reported as
`CS7036` inside `Logging.g.cs`, with no generator diagnostic on the attribute; one whose argument is optional compiles,
and the value is logged with the attribute's default classification, so it goes to another redactor than the one the
application configured for it.

```bash
./repro.sh    # .NET 10, Microsoft.Extensions.Telemetry.Abstractions 10.11.0; the first build is expected to fail
```

- `required-argument/`: `SensitiveAttribute(string level)`, used as `[Sensitive("Health")]`: `CS7036`.
- `optional-argument/`: `SensitiveAttribute(string level = "Public")`, used as `[Sensitive("Health")]`: the value goes
  to the fallback redactor, not the `Health` one.

Wanted: the attribute's own arguments used (or the classification read from the attribute data), or a generator
diagnostic on the attribute. See `repro-output-net10.0.txt`.
