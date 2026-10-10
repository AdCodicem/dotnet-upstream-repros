# Microsoft.AspNetCore.OpenApi: a `[DefaultValue]` of another type than its member fails the whole document

`[DefaultValue(5)]` on a `long`, `[DefaultValue(0.5)]` on a `decimal`, a string on a `DateOnly` or on a struct with
its own `JsonConverter`: generating the document throws `InvalidCastException`, at request time. `minimal/` is the
shortest case; `cases/` maps one document per case, with the forms that work.

```bash
./repro.sh                                                                 # .NET 10, Microsoft.AspNetCore.OpenApi 10.0.12
REPRO_TFM=net11.0 REPRO_OPENAPI_VERSION=11.0.0-rc.1.26425.128 ./repro.sh   # .NET 11 RC1
```

Expected: the document is generated. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
