# Request Delegate Generator: endpoints with the same parameter name and type share one interceptor (10.0)

With `EnableRequestDelegateGenerator`, `(Guid id)` and `([FromHeader(Name = "X-Id")] Guid id)` share one interceptor
on 10.0, so the second endpoint reads `id` from the route or query string. `[FromHeader(Name = "X-A")]` and
`[FromHeader(Name = "X-B")]` share one too, and the second reads `X-A`. Fixed on 11.0 by dotnet/aspnetcore#67591.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

The run with the generator off is the expected behaviour. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
