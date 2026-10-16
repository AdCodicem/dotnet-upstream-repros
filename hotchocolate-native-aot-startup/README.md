# Hot Chocolate: the smallest application aborts at start as a native AOT binary

One query type, written for HotChocolate's source generator (`[QueryType]`, `AddReproTypes()`), its executor built in process, one request. Under the JIT it answers `{"data":{"hello":"world"}}`. Published with `PublishAot=true`, the binary fails while building the schema: `A suitable constructor for type 'HotChocolate.Fetching.DataLoaderRootFieldTypeInterceptor' could not be located.` `AddGraphQL()` registers that interceptor by type (`TryAddTypeInterceptor<T>()`), `SchemaBuilder.Setup.Create` creates it with `ActivatorUtilities.CreateInstance` under an `UnconditionalSuppressMessage("IL2072")`, and the trimmer has removed its constructor. With `<TrimmerRootAssembly Include="HotChocolate.Types" />` (`-p:ReproRoot=true`) the binary answers.

```bash
./repro.sh                                # HotChocolate 16.6.8
REPRO_HC_VERSION=16.7.0-p.28 ./repro.sh   # the latest 16.7 preview: the same
```

Native publishing needs the platform's toolchain (on Windows, the Visual Studio C++ build tools; the script puts `vswhere.exe` on `PATH` when it is installed but not found). On Windows, clone to a short path: the linker fails with `LNK1104` when `obj/` lies too deep. See `repro-output-net10.0.txt` and `repro-output-net10.0-hotchocolate-16.7.0-p.28.txt`; the publish logs are left in `publish/`.
