# protobuf-net: a scalar custom serializer never gets packed lists, nor a member's `DataFormat`

`Quantity` is an `int` that crosses the wire as an `int32`, through a serializer declaring
`SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint` (`Name = "int32"` only names it in the export).
Compared with an `int` member:

1. a `List<Quantity>` is never written packed, even with `IsPacked = true`;
2. packed data for it cannot be read: what a proto3 peer sends for `repeated int32` (written here with Google.Protobuf)
   fails with `Invalid wire-type (String)`, where a `List<int>` reads it;
3. the proto3 export marks the `List<int>` `[packed = false]` and not the `List<Quantity>`, so a peer generated from it
   sends packed data the service cannot read;
4. `DataFormat = DataFormat.ZigZag` on a `Quantity` member is dropped silently: it is written as a plain varint, exported
   as `int32`, and a peer's `sint32` -3 (`10 05`) is read as 5.

```bash
./repro.sh
```

Expected: the list of a scalar serializer of a packable wire type behaves as the list of the primitive (packed on request,
packed data accepted on read, `[packed = false]` in the export when not packed), and a member's `DataFormat` either
reaches the serializer's wire type or is refused. See `repro-output-net10.0.txt`, on protobuf-net 3.4.30.
