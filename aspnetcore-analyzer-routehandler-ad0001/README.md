# RouteHandlerAnalyzer: NullReferenceException (AD0001) for a handler passed as a delegate creation or as a lambda under a cast

`RouteHandlerAnalyzer`, an analyzer of the ASP.NET Core reference pack, throws `NullReferenceException` on a `Map`
call whose handler is a delegate creation, `new Func<int, string>(Handlers.Size)`, or a lambda under a cast,
`(Func<int, string>)(size => ...)`. The build reports warning AD0001 for each such call, and fails under
`TreatWarningsAsErrors`. The Request Delegate Generator plays no part. Still fails on 11.0.0-rc.1.

```bash
./repro.sh                      # three builds on .NET 10: plain, generator on, warnings as errors
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: no AD0001, as for the same handler passed as a method group or as a lambda. See `repro-output-net10.0.txt`
and `repro-output-net11.0.txt`.

The messages give character offsets into `Program.cs`, so `.gitattributes` keeps that file in LF on every checkout.
