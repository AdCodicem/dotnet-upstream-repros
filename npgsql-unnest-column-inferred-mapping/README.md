# Npgsql: an unnest over an array column gets its type mapping inferred anyway

`NpgsqlTypeMappingPostprocessor` infers the type mapping of the array an `unnest` reads from the expressions that
reference its element, even when that array is a column, whose mapping comes from the model. Two consequences:

1. plain LINQ: `o.Tags.Union(o.Labels)` or `o.Tags.Except(o.Labels)` over a `text[]` and a `character varying(10)[]`
   column throws `Conflicting type mappings were inferred for column 'value'`;
2. a member translator plugin that returns the element column under another CLR type fails with an
   `ArgumentException` out of `NpgsqlArrayTypeMapping`.

SQLite translates both, and so does SQL Server on EF Core 11 (on 10.0, SQL Server fails the first one too).

```bash
./repro.sh                 # translation only (ToQueryString), no server
REPRO_DOCKER=1 ./repro.sh  # also runs the queries on a PostgreSQL 17 container it starts and removes
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 REPRO_NPGSQL_VERSION=11.0.0-rc.1.1 ./repro.sh   # .NET 11 RC1
```

`fix-check/` runs every query as shipped, then with a copy of the postprocessor that infers only for an `unnest` whose
array has no type mapping of its own, swapped in through `IQueryTranslationPostprocessorFactory`. See
`repro-output-net10.0.txt` and `repro-output-net11.0.txt`, both run with `REPRO_DOCKER=1`.
