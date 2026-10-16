# Swashbuckle.AspNetCore: a `[FromQuery]` model hiding a base property with another type fails the document

An MVC action takes `[FromQuery] DerivedQuery`, where `DerivedQuery` hides `BaseQuery.Code` (`string`) with
`public new int Code`. MVC binds the model, but generating the document throws `AmbiguousMatchException` from
`ApiParameterDescriptionExtensions.PropertyInfo`, which looks the property up with `Type.GetProperty(name)`. The same
on 10.3.0 and 6.9.0; reported in 2019 as domaindrivendev/Swashbuckle.AspNetCore#1193, closed for inactivity. Hiding
with the same type works.

```bash
./repro.sh                                       # .NET 10, Swashbuckle.AspNetCore 10.3.0 then 6.9.0
REPRO_SWASHBUCKLE_VERSIONS="10.3.0" ./repro.sh   # one version
```

Expected: the document is generated, with one `Code` query parameter of type `integer`. See
`repro-output-net10.0.txt`.
