# Request Delegate Generator: handlers that differ only by a default value share one interceptor

With `EnableRequestDelegateGenerator`, `(int page = 1)` and `(int page = 2)` share one interceptor, which casts the
second handler to the first one's delegate type and throws `InvalidCastException` at startup. Still fails on
11.0.0-rc.1.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: `page 1` and `page 2`, as with the generator off. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
