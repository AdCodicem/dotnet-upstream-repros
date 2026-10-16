# dotnet-upstream-repros

Minimal reproductions of bugs and API gaps reported to .NET repositories and to libraries of the .NET ecosystem.
Each folder is a standalone project that uses only the .NET SDK and the packages it names, with a `repro.sh` that runs
it from scratch and the output it printed (`repro-output-<tfm>.txt`), on .NET 10 and, where the table says so, on
.NET 11 RC1.

| Folder | Reported in | Reproduced on 10.0 | On 11.0.0-rc.1 |
|--------|-------------|--------------------|----------------|
| [`efcore-precompiled-converter-cast`](efcore-precompiled-converter-cast) | [dotnet/efcore#39174](https://github.com/dotnet/efcore/issues/39174) | Yes | Yes |
| [`efcore-precompiled-enum-parameter`](efcore-precompiled-enum-parameter) | [dotnet/efcore#33541](https://github.com/dotnet/efcore/issues/33541#issuecomment-6002610956) (comment) | Yes | Yes |
| [`efcore-tpc-complex-column-prefix`](efcore-tpc-complex-column-prefix) | [dotnet/efcore#39244](https://github.com/dotnet/efcore/issues/39244) | Not reachable: 10.0 maps no column there | Yes |
| [`efcore-primitive-collection-converter-null-element`](efcore-primitive-collection-converter-null-element) | [dotnet/efcore#39245](https://github.com/dotnet/efcore/issues/39245) | Yes | The read; the write is fixed |
| [`npgsql-unnest-column-inferred-mapping`](npgsql-unnest-column-inferred-mapping) | [npgsql/efcore.pg#3940](https://github.com/npgsql/efcore.pg/issues/3940) | Yes | Yes |
| [`aspnetcore-rdg-shared-interceptor`](aspnetcore-rdg-shared-interceptor) | [dotnet/aspnetcore#69676](https://github.com/dotnet/aspnetcore/issues/69676) (10.0 backport of #67591) | Yes | Fixed |
| [`aspnetcore-rdg-default-value`](aspnetcore-rdg-default-value) | [dotnet/aspnetcore#69675](https://github.com/dotnet/aspnetcore/issues/69675) | Yes | Yes |
| [`aspnetcore-rdg-cast-rebuilt-from-method`](aspnetcore-rdg-cast-rebuilt-from-method) | [dotnet/aspnetcore#69755](https://github.com/dotnet/aspnetcore/issues/69755) | Yes | Yes |
| [`aspnetcore-rdg-string-default-explicit-source`](aspnetcore-rdg-string-default-explicit-source) | [dotnet/aspnetcore#69756](https://github.com/dotnet/aspnetcore/issues/69756) | Yes | Yes |
| [`aspnetcore-rdg-mapfallback-overloads`](aspnetcore-rdg-mapfallback-overloads) | [dotnet/aspnetcore#69757](https://github.com/dotnet/aspnetcore/issues/69757) | Yes | Yes |
| [`aspnetcore-rdg-negative-zero-default`](aspnetcore-rdg-negative-zero-default) | [dotnet/aspnetcore#69758](https://github.com/dotnet/aspnetcore/issues/69758) | Yes | Yes |
| [`aspnetcore-rdg-mapfallback-order`](aspnetcore-rdg-mapfallback-order) | [dotnet/aspnetcore#69759](https://github.com/dotnet/aspnetcore/issues/69759) | Yes | Yes |
| [`aspnetcore-analyzer-routehandler-ad0001`](aspnetcore-analyzer-routehandler-ad0001) | [dotnet/aspnetcore#69760](https://github.com/dotnet/aspnetcore/issues/69760) | Yes | Yes |
| [`aspnetcore-openapi-default-value-type-mismatch`](aspnetcore-openapi-default-value-type-mismatch) | [dotnet/aspnetcore#60019](https://github.com/dotnet/aspnetcore/issues/60019#issuecomment-6095044674) (comment) | Yes | Yes |
| [`mcp-csharp-withtools-schema-options`](mcp-csharp-withtools-schema-options) | [modelcontextprotocol/csharp-sdk#1865](https://github.com/modelcontextprotocol/csharp-sdk/issues/1865#issuecomment-6095041485) (comment) | Yes | Not run |
| [`mcp-csharp-outputschema-required-null`](mcp-csharp-outputschema-required-null) | [modelcontextprotocol/csharp-sdk#1915](https://github.com/modelcontextprotocol/csharp-sdk/issues/1915) | Yes | Not run |
| [`mcp-csharp-delegating-tool-legacy-outputschema`](mcp-csharp-delegating-tool-legacy-outputschema) | [modelcontextprotocol/csharp-sdk#1916](https://github.com/modelcontextprotocol/csharp-sdk/issues/1916) | Yes | Not run |
| [`meai-response-format-schema-options`](meai-response-format-schema-options) | [dotnet/extensions#7821](https://github.com/dotnet/extensions/issues/7821) | Yes | Not run |
| [`agent-framework-runasync-schema-options`](agent-framework-runasync-schema-options) | [microsoft/agent-framework#9245](https://github.com/microsoft/agent-framework/issues/9245) | Yes | Not run |
| [`semantic-kernel-converter-message-swallowed`](semantic-kernel-converter-message-swallowed) | [microsoft/semantic-kernel#14575](https://github.com/microsoft/semantic-kernel/issues/14575) | Yes | Not run |
| [`extensions-genlogging-type-classification`](extensions-genlogging-type-classification) | [dotnet/extensions#7855](https://github.com/dotnet/extensions/issues/7855) | Yes | Not run |
| [`extensions-genlogging-logproperties-expansion`](extensions-genlogging-logproperties-expansion) | [dotnet/extensions#7856](https://github.com/dotnet/extensions/issues/7856) | Yes | Not run |
| [`extensions-genlogging-classification-attribute-arguments`](extensions-genlogging-classification-attribute-arguments) | [dotnet/extensions#7857](https://github.com/dotnet/extensions/issues/7857) | Yes | Not run |
| [`extensions-genlogging-transitive-stack-overflow`](extensions-genlogging-transitive-stack-overflow) | [dotnet/extensions#7858](https://github.com/dotnet/extensions/issues/7858) | Yes | Not run |
| [`aspnetcore-blazor-nullable-parsable`](aspnetcore-blazor-nullable-parsable) | [dotnet/aspnetcore#69901](https://github.com/dotnet/aspnetcore/issues/69901) | Yes | Yes (`Code?`; `Guid?` is fixed) |
| [`aspnetcore-jsonpatch-newtonsoft`](aspnetcore-jsonpatch-newtonsoft) | [dotnet/aspnetcore#69902](https://github.com/dotnet/aspnetcore/issues/69902), [dotnet/aspnetcore#69903](https://github.com/dotnet/aspnetcore/issues/69903), [dotnet/aspnetcore#69904](https://github.com/dotnet/aspnetcore/issues/69904), [dotnet/aspnetcore#68942](https://github.com/dotnet/aspnetcore/issues/68942#issuecomment-6225098788) (comment), [dotnet/aspnetcore#25942](https://github.com/dotnet/aspnetcore/issues/25942#issuecomment-6225099217) (comment) | Yes | Yes |
| [`aspnetcore-rdg-asparameters-nullable-constructor`](aspnetcore-rdg-asparameters-nullable-constructor) | [dotnet/aspnetcore#58953](https://github.com/dotnet/aspnetcore/issues/58953#issuecomment-6225098398) (comment) | Yes | Yes |
| [`runtime-regex-generator-culturename`](runtime-regex-generator-culturename) | [dotnet/runtime#135977](https://github.com/dotnet/runtime/issues/135977) | Yes | Yes |
| [`runtime-options-validator-display-name`](runtime-options-validator-display-name) | [dotnet/runtime#135978](https://github.com/dotnet/runtime/issues/135978) | Yes | Yes |
| [`runtime-stj-schema-converter-value-object`](runtime-stj-schema-converter-value-object) | [dotnet/runtime#105769](https://github.com/dotnet/runtime/issues/105769#issuecomment-6225163542) (comment) | Yes | Yes |
| [`efcore-compiled-nullable-comparer`](efcore-compiled-nullable-comparer) | [dotnet/efcore#39291](https://github.com/dotnet/efcore/issues/39291) | Yes | Yes |
| [`efcore-cosmos-contains-converted`](efcore-cosmos-contains-converted) | [dotnet/efcore#35092](https://github.com/dotnet/efcore/issues/35092#issuecomment-6225163999) (comment) | Yes | Yes |
| [`efcore-interceptor-reader-error`](efcore-interceptor-reader-error) | [dotnet/efcore#35229](https://github.com/dotnet/efcore/issues/35229#issuecomment-6225164337) (comment) | Yes | Yes |
| [`functions-worker-aspnetcore-analyzer-ad0001`](functions-worker-aspnetcore-analyzer-ad0001) | [Azure/azure-functions-dotnet-worker#3520](https://github.com/Azure/azure-functions-dotnet-worker/issues/3520) | Yes | Not run |
| [`hotchocolate-data-named-filter-convention-field-order`](hotchocolate-data-named-filter-convention-field-order) | [ChilliCream/graphql-platform#10565](https://github.com/ChilliCream/graphql-platform/issues/10565) | Yes | Not run |
| [`hotchocolate-native-aot-startup`](hotchocolate-native-aot-startup) | [ChilliCream/graphql-platform#10566](https://github.com/ChilliCream/graphql-platform/issues/10566) | Yes | Not run |
| [`hotchocolate-data-hasvalue-istrue-efcore`](hotchocolate-data-hasvalue-istrue-efcore) | [ChilliCream/graphql-platform#10567](https://github.com/ChilliCream/graphql-platform/issues/10567) | Yes | Not run |
| [`hotchocolate-data-custom-filter-operation-cost`](hotchocolate-data-custom-filter-operation-cost) | [ChilliCream/graphql-platform#10568](https://github.com/ChilliCream/graphql-platform/issues/10568) | Yes | Not run |
| [`swashbuckle-schemafilter-constructor-deadlock`](swashbuckle-schemafilter-constructor-deadlock) | [domaindrivendev/Swashbuckle.AspNetCore#4207](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/4207) | Yes | Not run |
| [`swashbuckle-fromquery-hidden-property-ambiguous`](swashbuckle-fromquery-hidden-property-ambiguous) | [domaindrivendev/Swashbuckle.AspNetCore#4208](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/4208) | Yes | Not run |
| [`protobuf-net-protomodel-scalar-serializer-nullable-map`](protobuf-net-protomodel-scalar-serializer-nullable-map) | [protobuf-net/protobuf-net#1371](https://github.com/protobuf-net/protobuf-net/issues/1371) | Yes | Not run |
| [`protobuf-net-protomodel-trim-warnings`](protobuf-net-protomodel-trim-warnings) | [protobuf-net/protobuf-net#1372](https://github.com/protobuf-net/protobuf-net/issues/1372) | Yes | Not run |
| [`protobuf-net-scalar-serializer-packed-dataformat`](protobuf-net-scalar-serializer-packed-dataformat) | [protobuf-net/protobuf-net#1373](https://github.com/protobuf-net/protobuf-net/issues/1373) | Yes | Not run |
| [`protobuf-net-getschema-surrogate-package-root-level`](protobuf-net-getschema-surrogate-package-root-level) | [protobuf-net/protobuf-net#1374](https://github.com/protobuf-net/protobuf-net/issues/1374) | Yes | Not run |
| [`protobuf-net-map-key-scalar-serializer`](protobuf-net-map-key-scalar-serializer) | [protobuf-net/protobuf-net#1352](https://github.com/protobuf-net/protobuf-net/issues/1352#issuecomment-6225269019) (comment) | Yes | Not run |
| [`akka-serializer-v2-generic-formatter`](akka-serializer-v2-generic-formatter) | [akkadotnet/akka.net#8821](https://github.com/akkadotnet/akka.net/issues/8821) | Yes (1.6.0-beta1 to beta3) | Not run |
| [`nswag-query-model-hidden-property`](nswag-query-model-hidden-property) | [RicoSuter/NSwag#5412](https://github.com/RicoSuter/NSwag/issues/5412) | Yes | Not run |
| [`nswag-minimal-api-newtonsoft-settings`](nswag-minimal-api-newtonsoft-settings) | [RicoSuter/NSwag#5413](https://github.com/RicoSuter/NSwag/issues/5413) | Yes | Not run |
| [`njsonschema-csharp-date-enum-duplicate-converter`](njsonschema-csharp-date-enum-duplicate-converter) | [RicoSuter/NJsonSchema#1945](https://github.com/RicoSuter/NJsonSchema/issues/1945) | Yes | Not run |
| [`njsonschema-validator-integer-beyond-int64`](njsonschema-validator-integer-beyond-int64) | [RicoSuter/NJsonSchema#568](https://github.com/RicoSuter/NJsonSchema/issues/568#issuecomment-6225273089) (comment) | Yes | Not run |

Running a repro needs bash (Git Bash works on Windows) and the .NET SDK of the target framework:

```bash
./efcore-precompiled-converter-cast/repro.sh                       # .NET 10
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 \
  ./efcore-precompiled-converter-cast/repro.sh                     # .NET 11 RC1 (the EF Core folders)
REPRO_TFM=net11.0 ./aspnetcore-rdg-default-value/repro.sh          # .NET 11 RC1 (the ASP.NET Core folders)
```

The two `mcp-csharp-*` folders with a `client/` also need Node.js: they run the TypeScript SDK's client against the
C# server.

The `efcore-precompiled-*` folders install `dotnet-ef` locally, under `.tools/`, at the same version as the packages,
and point `DOTNET_ROOT` at the `dotnet` on `PATH` when it is not set, so an SDK installed in a folder works too.

`npgsql-unnest-column-inferred-mapping` and `efcore-primitive-collection-converter-null-element` run without a
database server. With `REPRO_DOCKER=1` they also start PostgreSQL 17, or SQL Server 2022, in a container they remove,
for the part that needs one. `efcore-tpc-complex-column-prefix` runs on .NET 11 only.

The `hotchocolate-*` folders also keep the output of the HotChocolate 16.7 preview (`REPRO_HC_VERSION`).
`hotchocolate-native-aot-startup` and `protobuf-net-protomodel-trim-warnings` publish natively, which needs the
platform's native toolchain (the C++ build tools on Windows) and, on Windows, a short path.
