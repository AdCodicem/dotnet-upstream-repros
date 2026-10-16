# HotChocolate.Data: a field filtered through a named convention fails the schema when it comes first

With a default filter convention (`AddFiltering()`) and a named one (`AddFiltering("s")`), a field filtered through the named convention (`[UseFiltering(Scope = "s")]`) declared before a field filtered through the default one fails the schema at start: `The name 'IntOperationFilterInput' was already registered by another type.` (or `StringOperationFilterInput`, or `BookFilterInput`, depending on the types). Declared the other way around, the same two fields build, with `BookFilterInput` and `s_BookFilterInput`. The two fields need not filter the same type, and two named conventions together build in any order. Since C# lists a derived class's members before its base class's, a named field on a derived query class fails the same way. Projections play no part.

```bash
./repro.sh                                # HotChocolate 16.6.8
REPRO_HC_VERSION=16.7.0-p.28 ./repro.sh   # the latest 16.7 preview: the same
```

See `repro-output-net10.0.txt` and `repro-output-net10.0-hotchocolate-16.7.0-p.28.txt`.
