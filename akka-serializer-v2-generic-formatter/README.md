# Akka.Serialization.V2: a generic formatter loses its type arguments in the generated serializer (CS0305)

`[AkkaSerializerFormatter<Sku, StringValueFormatter<Sku>>]` registers a closed construction of a generic formatter, which
satisfies the attribute's `where TFormatter : IAkkaMessagePackFormatter<TTarget>` constraint. The source generator
writes the formatter's field and its construction without the type arguments,
`private readonly global::Repro.StringValueFormatter _akkaFormatter_Repro_Sku;`, and the build fails with CS0305 in the
generated file. No AKKASG diagnostic is reported. A closed, non-generic subclass of the same formatter
(`SkuFormatter : StringValueFormatter<Sku>`) compiles and round-trips. Fails on 1.6.0-beta1, beta2 and beta3.

```bash
./repro.sh                                   # the generic formatter, then the closed subclass, on 1.6.0-beta3
REPRO_AKKA_VERSION=1.6.0-beta2 ./repro.sh    # another version
```

Expected: the generated serializer declares `global::Repro.StringValueFormatter<global::Repro.Sku>` and the first build
succeeds, as the second one does. See `repro-output-net10.0.txt`.
