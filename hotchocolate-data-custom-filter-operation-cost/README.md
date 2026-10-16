# HotChocolate.Data: a filter operation type of the application's own cannot be weighed as expensive

HotChocolate's cost analysis weighs `contains`, `startsWith`, `endsWith` and their negations on `StringOperationFilterInputType` at `FilterCostOptions.DefaultExpensiveFilterOperationCost` (20), through `.Expensive()`, a `file`-scoped extension that sets the internal `CoreFieldFlags.FilterExpensiveOperationField`. An operation type of the application's own, even one deriving `StringOperationFilterInputType` and handled by the very same handlers, has no public way to set that flag: its `contains` costs `DefaultFilterOperationCost` (10). Writing `@cost(weight: 20)` by hand works, but no longer follows the application's `FilterCostOptions`.

```bash
./repro.sh                                # HotChocolate 16.6.8
REPRO_HC_VERSION=16.7.0-p.28 ./repro.sh   # the latest 16.7 preview: the same weights
```

Each case prints the operation type's fields as the schema publishes them and the cost reported for `{ books(where: { title: { contains: "a" } }) { title } }`. See `repro-output-net10.0.txt` and `repro-output-net10.0-hotchocolate-16.7.0-p.28.txt`.
