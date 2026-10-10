# Semantic Kernel: a parameter's `JsonConverter` error is replaced by "Object of type … cannot be converted"

When the `JsonConverter` of a native function's parameter type refuses the model's argument, `KernelFunctionFromMethod`
swallows its `JsonException` and passes the raw argument to `MethodInfo.Invoke`. The model receives a cast error
instead of the converter's reason. With `RetainArgumentTypes` at its default, a valid value never binds to a type
whose JSON form is a string and which has no `TypeConverter`.

```bash
./repro.sh    # .NET 10, Semantic Kernel 1.81.0; no network, an HttpMessageHandler stands for the OpenAI endpoint
```

Expected: the tool message carries the converter's message. See `repro-output-net10.0.txt`.
