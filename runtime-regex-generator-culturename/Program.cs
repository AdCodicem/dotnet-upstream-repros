using System.Globalization;
using System.Text.RegularExpressions;

// Usage: dotnet run -- <current culture at run time>
// Each [GeneratedRegex] below is compared with new Regex(pattern, options) constructed while
// CultureInfo.CurrentCulture is the declared cultureName, which is what cultureName documents.
CultureInfo runTime = CultureInfo.GetCultureInfo(args.Length > 0 ? args[0] : "en-US");
CultureInfo.CurrentCulture = runTime;
Console.WriteLine($"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}, CurrentCulture at run time: {runTime.Name}");
Console.WriteLine("Inputs: I (U+0049), \u0130 (U+0130), i (U+0069), \u0131 (U+0131); T = IsMatch true");
Console.WriteLine();

string[] inputs = ["I", "\u0130", "i", "\u0131"];

Console.WriteLine("1. Same pattern and options, different cultureName (generated code is used)");
Check(nameof(Patterns.First_enUS), Patterns.First_enUS, "en-US");
Check(nameof(Patterns.Second_trTR), Patterns.Second_trTR, "tr-TR");
Check(nameof(Patterns.First_trTR), Patterns.First_trTR, "tr-TR");
Check(nameof(Patterns.Second_Invariant), Patterns.Second_Invariant, "");
Console.WriteLine();

Console.WriteLine("2. RegexOptions.NonBacktracking (limited support: new Regex(...) without the culture)");
Check(nameof(Patterns.NonBacktracking_trTR), Patterns.NonBacktracking_trTR, "tr-TR");
Check(nameof(Patterns.NonBacktracking_enUS), Patterns.NonBacktracking_enUS, "en-US");
Check(nameof(Patterns.NonBacktracking_NoCultureName), Patterns.NonBacktracking_NoCultureName, "");
Console.WriteLine();

Console.WriteLine("3. Tree deeper than 40 (limited support: new Regex(...) without the culture)");
Check(nameof(Patterns.Deep_trTR), Patterns.Deep_trTR, "tr-TR");
Check(nameof(Patterns.Shallow_trTR), Patterns.Shallow_trTR, "tr-TR");
Console.WriteLine();

Console.WriteLine("4. Case-insensitive backreference (limited support: new Regex(...) without the culture)");
Check(nameof(Patterns.Backreference_trTR), Patterns.Backreference_trTR, "tr-TR", suffix: "xx");

void Check(string name, Regex generated, string cultureName, string suffix = "")
{
    CultureInfo declared = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
    CultureInfo saved = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = declared;
    Regex expected = new(generated.ToString(), generated.Options);
    CultureInfo.CurrentCulture = saved;

    string actualRow = string.Concat(inputs.Select(s => generated.IsMatch(s + suffix) ? "T" : "F"));
    string expectedRow = string.Concat(inputs.Select(s => expected.IsMatch(s + suffix) ? "T" : "F"));
    string label = cultureName.Length == 0 ? "(none: invariant)" : cultureName;
    string type = generated.GetType().Name;
    string impl = type == nameof(Regex) ? "new Regex(...)" : "generated type " + type[(type.LastIndexOf("__", StringComparison.Ordinal) + 2)..];
    Console.WriteLine($"   {name,-30} cultureName {label,-17} actual {actualRow}  expected {expectedRow}  {(actualRow == expectedRow ? "ok      " : "MISMATCH")}  {impl}");
}

static partial class Patterns
{
    // 1. Same pattern and options; only cultureName differs. The first declaration's code serves both.
    [GeneratedRegex("^i$", RegexOptions.IgnoreCase, "en-US")]
    public static partial Regex First_enUS { get; }

    [GeneratedRegex("^i$", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex Second_trTR { get; }

    [GeneratedRegex("^I$", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex First_trTR { get; }

    [GeneratedRegex("^I$", RegexOptions.IgnoreCase)]
    public static partial Regex Second_Invariant { get; }

    // 2. NonBacktracking: the generator emits a cached new Regex(pattern, options).
    [GeneratedRegex("^(?:i)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, "tr-TR")]
    public static partial Regex NonBacktracking_trTR { get; }

    [GeneratedRegex("^(?:I)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, "en-US")]
    public static partial Regex NonBacktracking_enUS { get; }

    [GeneratedRegex("^(?:i|j)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    public static partial Regex NonBacktracking_NoCultureName { get; }

    // 3. 41 nested capture groups: deeper than the generator's limit of 40, so it falls back the same way.
    [GeneratedRegex("^(((((((((((((((((((((((((((((((((((((((((i)))))))))))))))))))))))))))))))))))))))))$", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex Deep_trTR { get; }

    // The same with 10 groups: generated code, which honours tr-TR.
    [GeneratedRegex("^((((((((((i))))))))))$", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex Shallow_trTR { get; }

    // 4. A case-insensitive backreference is the third limited-support trigger.
    [GeneratedRegex(@"^i(x)\1$", RegexOptions.IgnoreCase, "tr-TR")]
    public static partial Regex Backreference_trTR { get; }
}
