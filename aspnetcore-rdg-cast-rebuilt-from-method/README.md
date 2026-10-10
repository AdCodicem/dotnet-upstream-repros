# Request Delegate Generator: the interceptor casts the handler to a delegate type rebuilt from its method

With `EnableRequestDelegateGenerator`, the interceptor casts the handler to the natural type of a lambda the generator
writes from the handler method's parameter types and default values, not to the type of the delegate passed. A single
endpoint whose delegate has another type throws `InvalidCastException` at its `Map` call: a lambda with a `params`
parameter, a method with a default value converted to `Func<int, string>`, a method converted to a named delegate
type. Still fails on 11.0.0-rc.1.

```bash
./repro.sh                      # generator off, then on, on .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

Expected: `params 2`, `page 1` and `size 5`, as with the generator off. See `repro-output-net10.0.txt` and
`repro-output-net11.0.txt`.
