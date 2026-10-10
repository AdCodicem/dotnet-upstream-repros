# Request Delegate Generator: the two MapFallback overloads share one interceptor, build error CS9144

With `EnableRequestDelegateGenerator`, `app.MapFallback(pattern, handler)` and `app.MapFallback(handler)` whose
handlers have the same signature share one interceptor, declared with the parameters of the first call. It cannot
intercept the other call, and the build fails with CS9144. Swapping the two calls moves the error to the other
overload. Still fails on 11.0.0-rc.1.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: the build succeeds and the run prints `api fallback` and `site fallback`, as with the generator off. See
`repro-output-net10.0.txt` and `repro-output-net11.0.txt`.

`repro.sh` builds with `-p:GenerateFullPaths=false`, so that the compiler names the generated file relative to this
folder. MSBuild looks for an error in the first 400 characters of a line, and the generated file sits 191 characters
below this folder: on Windows, with SDK 10.0.401, a plain `dotnet build` printed the CS9144 line when the full path
of that file was 375 characters long, and only `error MSB6006: "csc.exe" exited with code 1` when it was 383.
