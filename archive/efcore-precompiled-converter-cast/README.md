# Precompiled query: InvalidCastException on a value converter class

After `dotnet ef dbcontext optimize --precompile-queries --nativeaot`, materializing a property whose value converter
is a class (`RatingConverter : ValueConverter<Rating, int>`, or `EnumToStringConverter<Status>` behind
`HasConversion<string>()`) throws `InvalidCastException`. The compiled model rebuilds the converter as
`ValueConverter<Rating, int>`, while the precompiled shaper casts it to `RatingConverter`.

```bash
./repro.sh                       # control, optimize, precompiled run under the JIT
REPRO_PUBLISH_AOT=1 ./repro.sh   # also the native AOT binary (needs the native AOT prerequisites)
```

Expected: `Blog 1: Published` and `Post 1: rated 5` in every run. Actual: two `InvalidCastException` once the
queries are precompiled, under the JIT and in the native AOT binary. On 11.0.0-rc.1 the enum works and
`RatingConverter` still fails. See `repro-output-net10.0.txt` (run with `REPRO_PUBLISH_AOT=1`) and
`repro-output-net11.0.txt` (JIT only).
