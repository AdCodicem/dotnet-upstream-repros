# dotnet-upstream-repros

Minimal reproductions of bugs and API gaps reported to .NET repositories. Each folder is a standalone project that
uses only the .NET SDK and the packages it names, with a `repro.sh` that runs it from scratch and the output it
printed (`repro-output-<tfm>.txt`), on .NET 10 and, where the table says so, on .NET 11 RC1.

| Folder | Reported in | Reproduced on 10.0 | On 11.0.0-rc.1 |
|--------|-------------|--------------------|----------------|
| [`efcore-precompiled-converter-cast`](efcore-precompiled-converter-cast) | [dotnet/efcore#39174](https://github.com/dotnet/efcore/issues/39174) | Yes | Yes |
| [`efcore-precompiled-enum-parameter`](efcore-precompiled-enum-parameter) | [dotnet/efcore#33541](https://github.com/dotnet/efcore/issues/33541#issuecomment-6002610956) (comment) | Yes | Yes |
| [`aspnetcore-rdg-shared-interceptor`](aspnetcore-rdg-shared-interceptor) | [dotnet/aspnetcore#69676](https://github.com/dotnet/aspnetcore/issues/69676) (10.0 backport of #67591) | Yes | Fixed |
| [`aspnetcore-rdg-default-value`](aspnetcore-rdg-default-value) | [dotnet/aspnetcore#69675](https://github.com/dotnet/aspnetcore/issues/69675) | Yes | Yes |
| [`aspnetcore-rdg-cast-rebuilt-from-method`](aspnetcore-rdg-cast-rebuilt-from-method) | [dotnet/aspnetcore#69755](https://github.com/dotnet/aspnetcore/issues/69755) | Yes | Yes |
| [`aspnetcore-openapi-default-value-type-mismatch`](aspnetcore-openapi-default-value-type-mismatch) | [dotnet/aspnetcore#60019](https://github.com/dotnet/aspnetcore/issues/60019#issuecomment-6095044674) (comment) | Yes | Yes |
| [`mcp-csharp-withtools-schema-options`](mcp-csharp-withtools-schema-options) | [modelcontextprotocol/csharp-sdk#1865](https://github.com/modelcontextprotocol/csharp-sdk/issues/1865#issuecomment-6095041485) (comment) | Yes | Not run |
| [`mcp-csharp-outputschema-required-null`](mcp-csharp-outputschema-required-null) | [modelcontextprotocol/csharp-sdk#1915](https://github.com/modelcontextprotocol/csharp-sdk/issues/1915) | Yes | Not run |
| [`mcp-csharp-delegating-tool-legacy-outputschema`](mcp-csharp-delegating-tool-legacy-outputschema) | [modelcontextprotocol/csharp-sdk#1916](https://github.com/modelcontextprotocol/csharp-sdk/issues/1916) | Yes | Not run |
| [`meai-response-format-schema-options`](meai-response-format-schema-options) | [dotnet/extensions#7821](https://github.com/dotnet/extensions/issues/7821) | Yes | Not run |
| [`agent-framework-runasync-schema-options`](agent-framework-runasync-schema-options) | [microsoft/agent-framework#9245](https://github.com/microsoft/agent-framework/issues/9245) | Yes | Not run |
| [`semantic-kernel-converter-message-swallowed`](semantic-kernel-converter-message-swallowed) | [microsoft/semantic-kernel#14575](https://github.com/microsoft/semantic-kernel/issues/14575) | Yes | Not run |

Running a repro needs bash (Git Bash works on Windows) and the .NET SDK of the target framework:

```bash
./efcore-precompiled-converter-cast/repro.sh                       # .NET 10
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 \
  ./efcore-precompiled-converter-cast/repro.sh                     # .NET 11 RC1 (the EF Core folders)
REPRO_TFM=net11.0 ./aspnetcore-rdg-default-value/repro.sh          # .NET 11 RC1 (the ASP.NET Core folders)
```

The two `mcp-csharp-*` folders with a `client/` also need Node.js: they run the TypeScript SDK's client against the
C# server.

The EF Core folders install `dotnet-ef` locally, under `.tools/`, at the same version as the packages, and point
`DOTNET_ROOT` at the `dotnet` on `PATH` when it is not set, so an SDK installed in a folder works too.
