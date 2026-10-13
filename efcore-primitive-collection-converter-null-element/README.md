# Primitive collection: NullReferenceException when an element converter returns null

A `List<Quantity?>` whose elements have a `ValueConverter<Quantity?, int?>` storing `Quantity(0)` as `null`, and
reading `0` back as `null`. The same converter works on a scalar `Quantity?` property. On the elements, EF Core
10.0.12 throws `NullReferenceException` on `SaveChanges` (`JsonConvertedValueReaderWriter.ToJsonTyped`) and on read
(`JsonCastValueReaderWriter.FromJsonTyped`). On 11.0.0-rc.1 the write stores a JSON `null`; the read still throws.

```bash
./repro.sh                                                              # .NET 10, EF Core 10.0.12
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     # .NET 11 RC1
REPRO_DOCKER=1 ./repro.sh                                               # also writes through the SQL Server provider
```

Expected: lines 3 and 6 store and read a `null` element, as line 1 does for a scalar property and line 4 for an
element over `string`. `variants/` holds the same failure in other shapes: plain types (`List<int?>` with a
`ValueConverter<int?, int?>`), a converter built with `convertsNulls: true`, a collection parameter sent as one JSON
parameter (`ParameterTranslationMode.Parameter`), and the SQL Server provider. See `repro-output-net10.0.txt` (run with
`REPRO_DOCKER=1`) and `repro-output-net11.0.txt`.
