# Cosmos: Contains over a local collection of a value-converted type

`ids.Contains(x.Property)`, where `ids` is a local array and the property has a value converter. Over a struct converted to a string (`ValueConverter<Iban, string>`), translation throws `InvalidOperationException: Couldn't find array type mapping when applying item/array mappings`. Over a type the provider maps by itself (an enum with `HasConversion<string>()`, a `string` with a `ValueConverter<string, string>`), the query translates but the array parameter is sent without the converter applied (`[0,1]` against stored `"Open"`, `["eur","usd"]` against stored `"EUR"`), so it matches nothing. `==` on the same properties converts the parameter.

No Cosmos DB account is needed: `ToQueryString()` translates the query and prints the parameters as they would be sent.

```bash
./repro.sh                                                              # .NET 10, EF Core 10.0.13
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     # .NET 11 RC1
```

Expected: `ARRAY_CONTAINS(@p, c["..."])` with the parameter's elements converted, as for an unconverted `string` property (line 1). Actual: lines 2, 4 and 6. Known upstream: dotnet/efcore#35092, #34026, #36330. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
