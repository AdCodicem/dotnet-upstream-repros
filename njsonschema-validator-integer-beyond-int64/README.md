# NJsonSchema: JsonSchemaValidator throws InvalidCastException on a JSON integer beyond Int64

`JsonSchemaValidator.ValidateNumber` runs for any integer or real token, whatever the schema's type, and reads it with
`token.Value<decimal>()`, catching only `OverflowException`. Json.NET reads an integer beyond `long.MaxValue` as a
`BigInteger`, which `Convert.ChangeType` refuses with `InvalidCastException`, so validation throws instead of
returning errors, even under `{}` or `{ "type": "string" }`: a `ulong` above `long.MaxValue`, a `UInt128`, a
`BigInteger`. Confluent's `JsonSerializer<T>` and `JsonDeserializer<T>` run this validator on every message by default.

```bash
./repro.sh    # .NET 10, NJsonSchema 11.6.1
```

Expected: no exception; `valid` under `{}`, `{ "type": "integer" }` and `{ "type": "number" }`, `StringExpected` under
`{ "type": "string" }`, as for `9223372036854775807`. See `repro-output-net10.0.txt`.
