# Precompiled query: enum stored as a string bound by its number

After a plain `dotnet ef dbcontext optimize --precompile-queries` (no native AOT), a query comparing an enum mapped with
`HasConversion<string>()` to a captured variable binds `@status='1'` instead of `'Published'` and returns no rows,
without any error. The generated parameter looks its type mapping up by CLR type and store type, which loses the
property's converter (dotnet/efcore#33541).

```bash
./repro.sh
```

Expected: `Found 1 blog(s)` in both runs. Actual: `Found 0 blog(s)` once the query is precompiled. See
`repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
