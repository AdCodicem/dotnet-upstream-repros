# protobuf-net `[ProtoModel]`: `SerializerCache.Get<TSerializer, T>()` makes the trim and AOT analyzers warn for every member of `T`

A `[ProtoModel]` reaches a hand-written serializer through `SerializerCache.Get<QuantitySerializer, Quantity>()`, whose
`T` carries `[DynamicallyAccessedMembers(DynamicAccess.ContractType)]`: constructors, fields, properties, methods and
nested types, public and not. So every member of `Quantity`, which the serializer, and not reflection, reads and writes,
is kept, and each one marked `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`, or reached through a nested type,
is reported at every call the generated model makes: `IL2026`, `IL2111` and `IL3050`. The native binary runs and writes
the JIT's bytes; nothing reflects over `Quantity`.

```bash
./repro.sh                  # dotnet build (Roslyn analyzers), then dotnet publish (ILC) and the native binary
REPRO_NATIVE=0 ./repro.sh   # the Roslyn analyzers only
```

On Windows, run it from a short path: past `MAX_PATH`, the native link fails with `LNK1104` on `Repro.sourcelink`.

Expected: no warning for the members of `Quantity`, as with `Quantity` holding no other member. The `IL2104` and
`IL3053` of `protobuf-net.Core` itself are there either way. See `repro-output-net10.0.txt`, on protobuf-net 3.4.30.
