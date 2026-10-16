# NSwag: a minimal API application is described with Newtonsoft.Json settings when Microsoft.AspNetCore.Mvc.NewtonsoftJson can be loaded

`AddOpenApiDocument` picks Newtonsoft.Json's settings whenever `GetJsonSerializerSettings` returns some and MVC has no
`SystemTextJsonOutputFormatter`. `GetJsonSerializerSettings` resolves `IOptions<MvcNewtonsoftJsonOptions>`, which the
options system answers for any type, so it returns the package's default settings as soon as the assembly
`Microsoft.AspNetCore.Mvc.NewtonsoftJson` loads, whether or not `AddNewtonsoftJson()` was called. A minimal API
application has no MVC output formatter, so a mere reference to the package, often transitive, switches its document
to Newtonsoft.Json: `orderId` instead of the `order_id` its endpoints write, an enum of integers instead of the
strings they write, `ConfigureHttpJsonOptions` ignored.

```bash
./repro.sh    # .NET 10, NSwag.AspNetCore 14.7.1; built without, then with, a reference to Microsoft.AspNetCore.Mvc.NewtonsoftJson 10.0.13
```

Expected: both builds describe `Order` with `order_id` and `OrderStatus` as strings, as the endpoint writes it. See
`repro-output-net10.0.txt`.
