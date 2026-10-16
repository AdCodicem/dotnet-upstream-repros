using System.Diagnostics.CodeAnalysis;

/// <summary>A struct parsed from three upper-case letters, as a value object or a strongly typed identifier would be.</summary>
public readonly record struct Code(string Value) : IParsable<Code>
{
    public static Code Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result) ? result : throw new FormatException($"'{s}' is not a code.");

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Code result)
    {
        if (s is { Length: 3 } && s.All(char.IsAsciiLetterUpper))
        {
            result = new Code(s);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value;
}

/// <summary>
/// A struct whose TryParse accepts empty text, the way out #60498 describes: the empty field is mapped, but to a value,
/// since TryParse can only produce a Tag, never a null Tag?.
/// </summary>
public readonly record struct Tag(string Value) : IParsable<Tag>
{
    public static Tag Parse(string s, IFormatProvider? provider) => new(s);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Tag result)
    {
        result = new Tag(s ?? "");
        return true;
    }

    public override string ToString() => $"Tag(\"{Value}\")";
}
