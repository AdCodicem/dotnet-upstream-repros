# JSON Patch (Newtonsoft.Json): conversions outside the application's settings, exceptions instead of errors, `move`

Five behaviours of `Microsoft.AspNetCore.JsonPatch`, the Newtonsoft.Json-based package, when it applies a document:

1. `ConversionResultProvider.ConvertTo` reads a value back under `new JsonSerializerSettings { ContractResolver = ... }`:
   every other setting is dropped (`Converters`, `DateTimeZoneHandling`, `FloatParseHandling`, `DateParseHandling`...),
   and it catches every exception, so a converter's message becomes "The value '...' is invalid for target location.".
2. `copy` (`ConversionResultProvider.CopyTo`), a dictionary key (`DictionaryAdapter.TryConvertKey`) and `test` (the
   `JsonConvert.SerializeObject` comparison of each adapter's `TryTest`) ignore the document's `ContractResolver` too:
   `copy` writes a default instance, `test` passes on a different value, and a key the value path reads is refused.
3. `-` in a list that holds no element (and in any list for `test`) throws `ArgumentOutOfRangeException`.
4. An operation without `path`, or a `copy` or `move` without `from`, throws `ArgumentNullException`.
5. `move` adds the value at its `path` after removing it from its `from` failed (here a get-only property): with an
   error callback, as MVC's `ApplyTo(model, ModelState)` passes, the error is reported and the value copied.
   `ObjectAdapter.Move` of `Microsoft.AspNetCore.JsonPatch.SystemTextJson` has the same code.

Sections 3 and 4 are already tracked (dotnet/aspnetcore#68942, dotnet/aspnetcore#25942) and are kept here for the
exact exceptions. Still fails on 11.0.0-rc.1 (and with the 11.0.0-rc.2 package).

```bash
./repro.sh                      # .NET 10, Microsoft.AspNetCore.JsonPatch 10.0.13
REPRO_TFM=net11.0 ./repro.sh    # .NET 11, Microsoft.AspNetCore.JsonPatch 11.0.0-rc.1.26425.128
```

The document is read under the settings shown and given their `ContractResolver`, as MVC's
`NewtonsoftJsonPatchInputFormatter` does, and applied with an error callback, as MVC's `ApplyTo(model, ModelState)`
does. Each `body` line reads the same JSON as a plain request body under the same settings, for comparison.

Expected: each `patch` line gives what its `body` line gives (1); `backupSku` is `ABCD1234`, the key `ABCD1234` is
accepted and the `test` fails (2); a `JsonPatchError`, not an exception (3, 4); `note` stays `null`, as it does
without an error callback (5). See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`; the local UTC offset of
the machine that ran them is +02:00.
