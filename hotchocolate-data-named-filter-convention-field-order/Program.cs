// A field filtered through a named filter convention ([UseFiltering(Scope = "s")]) fails the schema at start when it
// comes before a field filtered through the default convention: "The name `...FilterInput` was already registered by
// another type.". Declared the other way around, the same two fields build, and answer. Projections play no part.
// Each case builds its own executor, in process, and prints what it answers.
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

await Run<DefaultThenNamed>("1. [UseFiltering] field, then [UseFiltering(Scope = \"s\")] field, both over Book");
await Run<NamedThenDefault>("2. The same two fields, the named one declared first");
await Run<NamedThenDefaultOtherType>("3. Named over Book declared first, default over Author (no type in common but Int and String)");
await Run<TwoNamedScopes>("4. Two named conventions, \"a\" then \"b\", over Book");
await Run<DerivedNamed>("5. Default field on a base class, named field on the derived query class (C# lists derived members first)");

static async Task Run<TQuery>(string title) where TQuery : class
{
    Console.WriteLine($"== {title}");
    var services = new ServiceCollection();
    services.AddGraphQL()
        .AddQueryType<TQuery>()
        .AddFiltering()
        .AddFiltering("s")
        .AddFiltering("a")
        .AddFiltering("b");
    try
    {
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();
        var inputs = executor.Schema.ToString().Split('\n').Where(l => l.StartsWith("input ") && l.Contains("Book")).Select(l => l[6..].Trim().TrimEnd('{').Trim());
        Console.WriteLine($"   ok, filter inputs over Book: {string.Join(", ", inputs)}");
    }
    catch (SchemaException exception)
    {
        foreach (var error in exception.Errors)
        {
            Console.WriteLine($"   !! SchemaException: {error.Message} ({error.TypeSystemObject?.GetType()})");
        }
    }
    Console.WriteLine();
}

public class Author
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

public static class Data
{
    public static IQueryable<Book> Books() => new[] { new Book { Id = 1, Title = "a" } }.AsQueryable();

    public static IQueryable<Author> Authors() => new[] { new Author { Id = 1, Name = "a" } }.AsQueryable();
}

public class DefaultThenNamed
{
    [UseFiltering]
    public IQueryable<Book> Books() => Data.Books();

    [UseFiltering(Scope = "s")]
    public IQueryable<Book> ScopedBooks() => Data.Books();
}

public class NamedThenDefault
{
    [UseFiltering(Scope = "s")]
    public IQueryable<Book> ScopedBooks() => Data.Books();

    [UseFiltering]
    public IQueryable<Book> Books() => Data.Books();
}

public class NamedThenDefaultOtherType
{
    [UseFiltering(Scope = "s")]
    public IQueryable<Book> ScopedBooks() => Data.Books();

    [UseFiltering]
    public IQueryable<Author> Authors() => Data.Authors();
}

public class TwoNamedScopes
{
    [UseFiltering(Scope = "a")]
    public IQueryable<Book> ABooks() => Data.Books();

    [UseFiltering(Scope = "b")]
    public IQueryable<Book> BBooks() => Data.Books();
}

public class BaseQuery
{
    [UseFiltering]
    public IQueryable<Book> Books() => Data.Books();
}

public class DerivedNamed : BaseQuery
{
    [UseFiltering(Scope = "s")]
    public IQueryable<Book> ScopedBooks() => Data.Books();
}
