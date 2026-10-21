# Swashbuckle.AspNetCore: a schema filter taking `ISerializerDataContractResolver` makes the document request hang

A schema filter registered with `c.SchemaFilter<T>()` is created by `ActivatorUtilities` while `SchemaGeneratorOptions`
is being configured. When its constructor takes a service whose factory reads `IOptions<SchemaGeneratorOptions>.Value`,
the options are configured again from inside their own configuration: the recursion runs until the dependency injection
container's `StackGuard` moves it to another thread, which waits on a lock the first thread holds. The request for
`swagger.json` never answers, with no exception and no log entry.

`SchemaGeneratorOptions` and `ISchemaGenerator` hang on every 10.x version tried (10.0.0, 10.2.3, 10.3.0). `ISerializerDataContractResolver` hangs
from 10.3.0, whose factory now reads the options (domaindrivendev/Swashbuckle.AspNetCore#3998); it works on 10.2.3.
`IOptions<SchemaGeneratorOptions>` works, its value being read later. Operation and document filters, which
`ConfigureSwaggerGeneratorOptions` creates the same way, hang when they take `SwaggerGeneratorOptions` or
`ISwaggerProvider`, and work when they take `ISchemaGenerator`.

```bash
./repro.sh                                       # .NET 10, Swashbuckle.AspNetCore 10.3.0 then 10.2.3
REPRO_SWASHBUCKLE_VERSIONS="10.3.0" ./repro.sh   # one version
```

Each case waits 10 seconds for the document. Expected: every case answers 200, or a case that cannot be built fails
with an exception. See `repro-output-net10.0.txt`.
