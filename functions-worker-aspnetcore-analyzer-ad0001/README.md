# Azure Functions worker: `RegistrationExpectedInASPNetIntegration` throws NullReferenceException (AD0001)

`RegistrationExpectedInASPNetIntegration` (AZFW0014), the analyzer of
`Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Analyzers` 1.0.4, throws `NullReferenceException` on a
file declaring `Main` (or top-level statements) whenever a `ConfigureFunctionsWorkerDefaults` call starts an indented
statement, `hostBuilder.ConfigureFunctionsWorkerDefaults();`. The build reports warning AD0001, and fails under
`TreatWarningsAsErrors`, even when the file also calls `ConfigureFunctionsWebApplication` and nothing should be
reported; where AZFW0014 is due, it is lost. The same call with its result assigned is analysed as designed.

```bash
./repro.sh                      # four builds on .NET 10
```

Expected: no diagnostic for `Cases/if-else.cs`, AZFW0014 for `Cases/statement.cs` as for `Cases/assigned.cs` and
`Cases/column0.cs`, the same statement without its indentation. `net11.0` is not run: the Worker SDK refuses it
with `AzureFunctionsVersion` v4. See
`repro-output-net10.0.txt`.
