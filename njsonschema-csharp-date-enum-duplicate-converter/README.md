# NJsonSchema C# generator: two JsonConverter attributes on an enum of `format: date` strings (CS0579)

`Class.liquid` writes the enum converter when `property.IsStringEnum` and the `DateFormatConverter` when
`property.IsDate and UseDateFormatConverter`. Both hold for a property whose schema is a closed set of
`format: date` strings, so the property gets two `[JsonConverter]` attributes, and the generated code does not
compile: CS0579, under Newtonsoft.Json and System.Text.Json alike, from NJsonSchema's `CSharpGenerator` and from
NSwag's `CSharpClientGenerator`, with the default settings.

```bash
./repro.sh    # .NET 10, NSwag.CodeGeneration.CSharp 14.7.1 (NJsonSchema.CodeGeneration.CSharp 11.6.1)
```

`repro.sh` generates `client/*.g.cs` from `holiday.schema.json` and `openapi.json`, which hold the same schemas, then
compiles `client/Client.csproj`. The two generators run in separate processes: in one, the second writes no type (see
`njsonschema-template-cache-shared-key`).

Expected: one converter, the enum's, on `Day`; `PlainDate` and `Color` keep theirs. See `repro-output-net10.0.txt`.
