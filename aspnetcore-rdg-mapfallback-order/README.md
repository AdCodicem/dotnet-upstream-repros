# Request Delegate Generator: an intercepted MapFallback endpoint has order 0, not int.MaxValue

With `EnableRequestDelegateGenerator`, the endpoint of an intercepted `MapFallback` call is registered as an ordinary
route handler: its order is 0 instead of `int.MaxValue`, and it loses the `Fallback` prefix of its display name and
its fallback metadata. The fallback no longer sorts after the endpoints of order 0, and the route precedence decides:
here `{*path:nonfile}` wins over `MapGet("/{*path}", ...)`, which answers with the generator off. Still fails on
11.0.0-rc.1.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: the fallback endpoint has order 2147483647, the display name `Fallback {*path:nonfile}` and its fallback
metadata, and `GET /missing` returns `catch-all`, as with the generator off. See `repro-output-net10.0.txt` and
`repro-output-net11.0.txt`.
