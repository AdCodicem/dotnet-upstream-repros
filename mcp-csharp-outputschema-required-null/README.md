# MCP C# SDK: `outputSchema` requires a nullable property that `structuredContent` omits when null

With `UseStructuredContent = true`, a nullable property bound to a constructor parameter without a default value, or
declared `required`, is listed in `outputSchema.required`. The SDK's default serializer options drop it when it is
null, and a client that validates the result refuses it. The TypeScript SDK's client does (`client/`).

```bash
./repro.sh    # .NET 10, ModelContextProtocol 2.2.0, and Node.js for @modelcontextprotocol/sdk 1.32.1
```

Expected: every call validates, as with serializer options that keep nulls (last block). See
`repro-output-net10.0.txt`.
