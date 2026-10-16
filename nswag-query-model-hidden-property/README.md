# NSwag: AmbiguousMatchException for an MVC query model hiding an inherited property with one of another type

`AspNetCoreOpenApiDocumentGenerator` fails the whole document with `AmbiguousMatchException` when an action binds a
`[FromQuery]` model whose class hides an inherited property with `new` and another type (base `string Code`, derived
`new int Code`). `OperationParameterProcessor` looks the property up with
`ContainerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)`, which reflection refuses when
two public properties of that name differ by type. MVC binds the model; a hiding property of the same type works.

```bash
./repro.sh    # .NET 10, NSwag.AspNetCore 14.7.1
```

Expected: the document is generated, with a query parameter `Code` of type `integer`, the property MVC binds. See
`repro-output-net10.0.txt`.
