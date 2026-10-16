# Request Delegate Generator: CS8639 for an `[AsParameters]` type whose constructor takes a nullable reference type

With `EnableRequestDelegateGenerator`, an `[AsParameters]` type bound through its constructor (a positional record, a
class with a primary constructor) does not compile when one constructor parameter is a nullable reference type
(`string?`, `Uri?`, `string[]?`): the generated metadata looks the constructor up with `typeof(string?)`, and the build
fails with CS8639 in `GeneratedRouteBuilderExtensions.g.cs`, once per property of the type, the non-nullable ones
included. The same types bind with the generator off. Still fails on 11.0.0-rc.1.

```bash
./repro.sh                      # generator off (runs), then on (does not compile), on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: the build succeeds and the endpoints answer as with the generator off. See `repro-output-net10.0.txt` and
`repro-output-net11.0.txt`.
