# TPC: an inherited complex property's columns lose their prefix in a leaf table

EF Core 11 maps a complex property declared on the root of a TPC hierarchy to the leaf tables. There, its columns
are named without the complex property's prefix (`Zip`, where TPH, TPT and a concrete root's own table have
`Shipping_Zip`). Two inherited properties of the same type are told apart by the declaring entity type's name
(`Vessel_Zip` and `Zip`), and which one keeps the bare name depends on the rest of the model: adding `Billing` next
to `Shipping` hands the existing `Zip` column to `Billing`, so the stored shipping zip is read back as the billing zip.

```bash
./repro.sh    # .NET 11 RC1 (EF Core 10 maps no column there at all)
```

Expected: `Shipping_Zip` and `Billing_Zip` in the leaf table, whatever else the model holds, and `Shipping.Zip =
'75001'` after the migration. Actual: `Zip`, then `Vessel_Zip` and `Zip`, and `Shipping.Zip = ''` with `Billing.Zip =
'75001'`. `TpcExplicitNamesContext` is the workaround: column names set on the root. See `repro-output-net11.0.txt`.
