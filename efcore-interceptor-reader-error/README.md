# Interceptors: a server error raised while reading a query's rows reaches no interceptor

`IDbCommandInterceptor.CommandFailed` fires when the database error is raised by `ExecuteReader`. When the same error is raised while EF Core reads the rows (`DbDataReader.Read`), no interceptor method sees it: the interceptor gets `ReaderExecuted`, `DataReaderClosing` and `DataReaderDisposing` as for a success, while the logger receives `QueryIterationFailed` and the caller gets the exception. SQLite raises `integer overflow` for `abs(-9223372036854775808)` on the row it reaches, so ordering the same two rows one way or the other moves the error from `ExecuteReader` to `Read`.

```bash
./repro.sh                                                              # .NET 10, EF Core 10.0.13
REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     # .NET 11 RC1
```

The same applies to a `SaveChanges` batch whose later statement fails (dotnet/efcore#35229, SQL Server: the error is raised by `NextResult` while the batch is consumed, and reaches `ISaveChangesInterceptor.SaveChangesFailed` only). SQLite does not batch `SaveChanges` commands, so that case is not in this repro. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
