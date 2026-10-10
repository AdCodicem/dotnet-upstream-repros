# MCP C# SDK: a tool behind `DelegatingMcpServerTool` advertises a non-object `outputSchema` to older clients

For a client on a protocol version before 2026-07-28, the server lists the SDK's own tool with its `outputSchema`
wrapped in `{"result": …}`, and the same tool behind a pass-through `DelegatingMcpServerTool` with the bare schema,
while both return wrapped content. The TypeScript SDK's client (`client/`) then rejects the whole `tools/list`.

```bash
./repro.sh    # .NET 10, ModelContextProtocol 2.2.0, Node.js for @modelcontextprotocol/sdk 1.32.1; then 1.4.1 as a control
```

Expected: `count_wrapped` is listed like `count`, as it is on ModelContextProtocol 1.4.1 (last block). See
`repro-output-net10.0.txt`.
