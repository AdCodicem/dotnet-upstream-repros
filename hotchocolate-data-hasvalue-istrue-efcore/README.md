# HotChocolate.Data: `FilterExpressionBuilder.HasValue` builds `IsTrue(x.HasValue)`, which EF Core does not translate

`FilterExpressionBuilder` is the public helper a custom filter operation handler builds its expression with. Its `HasValue` (and so `HasValueAndAlso`) wraps `Nullable<T>.HasValue` in `Expression.IsTrue`, a `UnaryExpression` of node type `IsTrue` that EF Core does not translate. `NotNull` and `NotNullAndAlso` (`x != null`) translate to `IS NOT NULL`, and so does `HasValue` without `IsTrue`. In memory, all four agree.

```bash
./repro.sh                                # HotChocolate 16.6.8, EF Core Sqlite 10.0.13, an in-memory SQLite database
REPRO_HC_VERSION=16.7.0-p.28 ./repro.sh   # the latest 16.7 preview: the same
```

See `repro-output-net10.0.txt` and `repro-output-net10.0-hotchocolate-16.7.0-p.28.txt`.
