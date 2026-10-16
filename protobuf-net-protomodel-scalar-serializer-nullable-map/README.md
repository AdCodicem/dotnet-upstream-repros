# protobuf-net `[ProtoModel]`: a nullable or dictionary member of a type with a scalar serializer does not compile

A struct marked `[ProtoContract(Serializer = typeof(QuantitySerializer))]`, whose serializer declares
`SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint`, works in a `[ProtoModel]` as a plain member and as
a `List<Quantity>`, with the bytes of the runtime model. Add a `Quantity?` member, a `Dictionary<string, Quantity>` or a
`Dictionary<Quantity, string>`, and the generated `AppModel.ProtoModel.g.cs` fails to compile with `CS1503`; no `PBN`
diagnostic is reported. The runtime model serializes all three.

```bash
./repro.sh    # builds the model four times: without, then with each of the three members
```

Expected: either the generated code compiles and writes the runtime model's bytes, or the member is refused with a
`PBN3003` diagnostic (as the generator intends for a map, see `ProtoModelGenerator.Parse.cs`), never a compile error in
the generated file. See `repro-output-net10.0.txt`, on protobuf-net 3.4.30.
