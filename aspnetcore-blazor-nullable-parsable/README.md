# Blazor static form mapping: an empty field refuses a nullable custom `IParsable<T>` struct

A statically rendered `EditForm` (`[SupplyParameterFromForm]`, `FormName`) is posted with every input left empty, as
a browser posts it (`Model.Code=&Model.Number=&Model.Id=&Model.Tag=`). `int?` maps to `null`, but `Code?`, where `Code`
is a struct implementing `IParsable<Code>`, gets the mapping error "The value '' is not valid for 'Code'." and the
form is invalid. The form mapping's `NullableConverter<T>` turns an empty value into `null` only for the types of an
allowlist (#60498), and otherwise hands the empty text to `T.TryParse`, which can only return a `T`: `Tag`, whose
`TryParse` accepts empty text, maps to `Tag("")`, never to `null`. On 10.0, `Guid?` is refused too (#66607, fixed in
11.0 by #66625). Still fails for `Code?` on 11.0.0-rc.1.

```bash
./repro.sh                      # .NET 10
REPRO_TFM=net11.0 ./repro.sh    # .NET 11
```

Expected: `Code` and `Id` are `null` with no mapping error, as `Number` is, and as an interactive `InputBase<Code?>`
binds empty text. See `repro-output-net10.0.txt` and `repro-output-net11.0.txt`.
