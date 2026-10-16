# Compiled model: CS0453 for a struct's ValueComparer applied to a nullable property

A `ValueConverter<Qty, int>` and a `ValueComparer<Qty>` (`QtyComparer`) for a `readonly record struct Qty`, configured with `Properties<Qty>().HaveConversion<QtyConverter, QtyComparer>()`, also apply to a `Qty?` property. At run time EF Core wraps the comparer in `NullableValueComparer<Qty>`. `dotnet ef dbcontext optimize` writes `new NullableValueComparer<QtyComparer>(new QtyComparer())` instead, which does not compile: CS0453, `QtyComparer` is not a non-nullable value type. The same happens with `Property(o => o.Backordered).HasConversion<QtyConverter, QtyComparer>()`.

```bash
./repro.sh                                                              # .NET 10, EF Core 10.0.13
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     # .NET 11 RC1
```

Expected: the compiled model builds and runs as the model built at run time does, with `new NullableValueComparer<Qty>(new QtyComparer())` (editing the generated line to that builds and runs). Actual: CS0453. The last step is the workaround: a `ValueComparer<Qty?>` of its own for `Qty?` (`dotnet run -p:Workaround=true`, or `Workaround=true` in the environment for `dotnet ef`). See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
