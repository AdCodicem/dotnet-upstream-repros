# protobuf-net `GetSchema`: a surrogated dictionary key throws, the package guess counts custom-serializer types, a root primitive is exported at the model's level but written at Level200

1. `Cents`, `[ProtoContract(Surrogate = typeof(long))]`, serializes as a member, a list, a dictionary value and a
   dictionary key, and exports as the first three; `GetSchema` on a `Dictionary<Cents, int>` throws
   `InvalidOperationException: Failed to compare two elements in the array.`, inner
   `ArgumentException: Data of this type has inbuilt behaviour, and cannot be added to a model in this way: System.Int64`.
2. A message in `Shop` with a member served by a custom serializer in `Shop.Values` loses its `package Shop;` line: the
   guess counts the namespace of a type the file never declares.
3. On a model whose `DefaultCompatibilityLevel` is `Level300`, `GetSchema` for a root `Guid` exports
   `message Guid { string value = 1; }`, but `Serialize` writes the `Level200` form, a `.bcl.Guid` in field 1, and cannot
   read the exported shape back (`Invalid wire-type (7)`). The same holds for a root `DateTime`, `TimeSpan` and `decimal`.

```bash
./repro.sh
```

Expected: the dictionary exports; the package line comes from the types the file declares; a root primitive is exported
as it is written. See `repro-output-net10.0.txt`, on protobuf-net 3.4.30.
