# Agent Framework: `AIAgent.RunAsync<T>` takes no `AIJsonSchemaCreateOptions`

`RunAsync<T>` creates its structured-output schema with `ChatResponseFormat.ForJsonSchema<T>(serializerOptions)` and
replaces a `ResponseFormat` set on `AgentRunOptions`, so a type that a custom `JsonConverter` serializes is described
as `true`.

```bash
./repro.sh    # .NET 10, Microsoft.Agents.AI 1.24.0; no network, a scripted IChatClient stands for the model
```

Wanted: the schema printed under "Wanted", from `RunAsync<T>()`. See `repro-output-net10.0.txt`.
