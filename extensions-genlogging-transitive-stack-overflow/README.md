# Microsoft.Gen.Logging: `[LogProperties(Transitive = true)]` overflows the compiler's stack on a cycle through an inherited property

`LOGGEN021` reports a cycle through a property the type declares. Through a property declared on a base class, whose
type is the derived class, the generator recurses until the compiler process dies with `Stack overflow`. A property of
type `System.Type` reaches such a cycle through the reflection types' inherited properties.

```bash
./repro.sh    # .NET 10, Microsoft.Extensions.Telemetry.Abstractions 10.11.0; each build is expected to fail
```

- `declared/`: the cycle through a declared property, `LOGGEN021` as expected.
- `inherited/`: the same cycle through an inherited property: `Stack overflow`.
- `system-type/`: a `System.Type` property: `Stack overflow`.

The script builds with `--disable-build-servers`; through the shared compiler server, the server process dies the same
way and MSBuild falls back to `csc`, which dies too. Wanted: `LOGGEN021` for both. See `repro-output-net10.0.txt`.
