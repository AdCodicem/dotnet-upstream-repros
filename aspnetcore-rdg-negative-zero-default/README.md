# Request Delegate Generator: a `double` or `decimal` default of negative zero loses its sign

With `EnableRequestDelegateGenerator`, `(double d = -0.0)` receives `+0.0` when the value is absent from the request:
the generated fallback is `d_parsed_temp = -0;`, an `int` expression. A `decimal` default of `-0.0m` is written
`0.0M` and loses its sign too; a `float` keeps it (`-0F`). Still the same on 11.0.0-rc.1.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: `-0.0`, `-0.0f` and `-0.0m`, as with the generator off. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
