# Microsoft.Extensions.AI: structured output takes no `AIJsonSchemaCreateOptions`

`AIFunctionFactory` accepts `AIJsonSchemaCreateOptions` for a tool's schema. `ChatResponseFormat.ForJsonSchema<T>()`
and `GetResponseAsync<T>()` do not, and `GetResponseAsync<T>()` replaces a `ResponseFormat` the caller set, so a type
that a custom `JsonConverter` serializes is described as `true` in a structured-output schema.

```bash
./repro.sh    # .NET 10, Microsoft.Extensions.AI 10.10.0; no network, a scripted IChatClient stands for the model
```

Wanted: the schema printed under "Wanted", from `GetResponseAsync<T>()`. See `repro-output-net10.0.txt`.
