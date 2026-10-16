# JsonSchemaExporter: a single-value struct with a converter of its own

`Iban` is a struct a `JsonConverter<Iban>` writes as a JSON string. `JsonSchemaExporter` describes it as `true`, its
nullable form as `true` too (the `null` it accepts is not said), and a `List<Iban>` or a `Dictionary<string, Iban>`
without `items` or `additionalProperties`; `TransformSchemaNode` is called for the property holding the list or the
dictionary, but never for their elements, so a transform that describes `Iban` has to recognise every container and
rebuild `items` and `additionalProperties` from the container's `JsonTypeInfo`. Context for
[dotnet/runtime#105769](https://github.com/dotnet/runtime/issues/105769). The same on 11.0.0-rc.1.

```bash
./repro.sh                      # .NET 10
REPRO_TFM=net11.0 ./repro.sh    # .NET 11
```

See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
