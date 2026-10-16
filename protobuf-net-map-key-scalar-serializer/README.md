# protobuf-net: a dictionary key served by a scalar serializer is never a protobuf map

`Sku` is a string that crosses the wire as a string, through a serializer declaring
`SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString`. A `Dictionary<Sku, int>` writes the bytes of a
`Dictionary<string, int>`, but `RepeatedSerializerStub.IsValidProtobufMap` refuses the key type, so:

1. it is exported as `repeated KeyValuePair_string_Int32` rather than `map<string,int32>`;
2. a key sent twice throws `ArgumentException` on read, where the map keeps the last value, as the protobuf spec asks;
3. two such dictionaries over two key types exported under the same name declare the same entry message twice, which
   `protoc` refuses.

```bash
./repro.sh    # also runs the protoc of Grpc.Tools 2.84.0 on the third export
```

Expected: a key whose serializer is scalar with a wire type a map key can have is treated as that key, as the bytes
already are. See `repro-output-net10.0.txt`, on protobuf-net 3.4.30.
