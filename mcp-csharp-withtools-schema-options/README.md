# MCP C# SDK: `WithTools` takes no `McpServerToolCreateOptions`

`McpServerToolCreateOptions.SchemaCreateOptions` can describe a type that a custom `JsonConverter` serializes, but no
`WithTools` overload passes it: the tool's schemas say `true` for such a type unless each tool is built by hand with
`McpServerTool.Create`.

```bash
./repro.sh    # .NET 10, ModelContextProtocol 2.2.0
```

Wanted: the schemas of the second block, from `WithTools<T>()`. See `repro-output-net10.0.txt`.
