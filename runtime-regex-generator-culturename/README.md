# Regex source generator: `cultureName` is ignored by deduplication and dropped by the fallback

`[GeneratedRegex]` takes a `cultureName` that decides the case-insensitive mappings (the Turkish `I`/`ı` and `İ`/`i`),
and an empty one documents the invariant culture. The generator loses it in two places:

1. It emits one implementation per pattern, options and timeout, without the culture: of two declarations that differ
   only by `cultureName`, the second returns the first one's instance and matches with the first one's culture.
2. When it cannot generate code (`RegexOptions.NonBacktracking`, a tree deeper than 40, a case-insensitive
   backreference), it emits `new Regex(pattern, options)` without the culture, so the instance uses
   `CultureInfo.CurrentCulture` at the moment its type initializes, whatever `cultureName` says.

Still fails on 11.0.0-rc.1.

```bash
./repro.sh                      # .NET 10
REPRO_TFM=net11.0 ./repro.sh    # the same on .NET 11
```

The program runs twice, with `CurrentCulture` `en-US` then `tr-TR`. For each declaration it prints `IsMatch` for
`I`, `İ`, `i` and `ı` (`T`/`F`), and the expected row: `new Regex(pattern, options)` built while `CurrentCulture` is the
declared culture (invariant when there is none). The difference does not depend on ICU or NLS: `Regex` uses its own
case table (`RegexCaseEquivalences`), with three behaviours, invariant (`[Ii]`), non-Turkish (`[Ii\u0130]`) and
Turkish (`[i\u0130]`, `[I\u0131]`).

Expected: every row `ok`, whatever `CurrentCulture` is at run time. See `repro-output-net10.0.txt` and
`repro-output-net11.0.txt`: `Second_trTR` and `Second_Invariant` match like the declaration before them in both runs,
and the five `new Regex(...)` rows follow `CurrentCulture` instead of `cultureName` (each is `ok` only in the run whose
`CurrentCulture` happens to give the same mappings).
