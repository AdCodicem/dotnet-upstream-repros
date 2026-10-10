# Request Delegate Generator: a string bound through an explicit attribute loses its default value

With `EnableRequestDelegateGenerator`, `[FromQuery] string fromQuery = "asc"`, `[FromHeader] string fromHeader = "asc"`
and `[FromRoute] string fromRoute = "asc"` receive `null` when the request carries no value, where the
reflection-based binding passes `"asc"`. The same string without an attribute keeps its default, and so does an `int`
under `[FromQuery]`. Still fails on 11.0.0-rc.1.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: `"asc"` for `/query`, `/header` and `/route`, as with the generator off. The last step of the script prints
the fallback the generator wrote for each string parameter. See `repro-output-net10.0.txt` and
`repro-output-net11.0.txt`.
