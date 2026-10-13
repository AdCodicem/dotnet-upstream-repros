// NpgsqlTypeMappingPostprocessor infers the type mapping of the array an unnest reads from the columns referencing its
// element, even when that array is a column, which already has its mapping from the model. Two ways to see it:
//   1. plain LINQ: a set operation over two array columns whose element mappings differ only by facets
//      (text[] and character varying(10)[]) fails with "Conflicting type mappings were inferred for column 'value'";
//   2. a member translator plugin that returns the element column typed as another CLR type (here: `.Value` of a
//      value-converted struct, read as the column) fails with an ArgumentException out of NpgsqlArrayTypeMapping.
// The same queries are translated on SQL Server and SQLite. Set REPRO_POSTGRES to a connection string to also run them
// on a server.
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

var postgres = Environment.GetEnvironmentVariable("REPRO_POSTGRES");
var stack = args.Contains("--stack"); // print each failure on Npgsql in full

var prefix = "FR";
short least = 2;
var plain = new (string Name, Func<ShopContext, IQueryable<Order>> Query)[]
{
    ("o.Tags.Union(o.Labels).Contains(\"x\")", db => db.Orders.Where(o => o.Tags.Union(o.Labels).Contains("x"))),
    ("o.Tags.Except(o.Labels).Any()", db => db.Orders.Where(o => o.Tags.Except(o.Labels).Any())),
};
var withPlugin = new (string Name, Func<ShopContext, IQueryable<Order>> Query)[]
{
    ("o.Code.Value.StartsWith(prefix)", db => db.Orders.Where(o => o.Code.Value.StartsWith(prefix))),
    ("o.Codes.Any(c => c.Value.StartsWith(prefix))", db => db.Orders.Where(o => o.Codes.Any(c => c.Value.StartsWith(prefix)))),
    ("o.Quantities.Any(q => q.Value > least)", db => db.Orders.Where(o => o.Quantities.Any(q => q.Value > least))),
};

foreach (var (title, queries, plugin) in new[]
{
    ("1. Plain LINQ, no plugin", plain, (TranslationMode?)null),
    ("2. With the member translator plugin (the column re-typed)", withPlugin, TranslationMode.Column),
    ("3. With the plugin writing a CAST instead (workaround)", withPlugin, TranslationMode.Cast),
})
{
    Console.WriteLine($"######## {title}");
    foreach (var provider in new[] { "Npgsql", "SqlServer", "Sqlite" })
    {
        Console.WriteLine($"== {provider}");
        foreach (var (name, query) in queries)
        {
            using var db = new ShopContext(provider, plugin);
            Console.WriteLine($"   {name}");
            try
            {
                var sql = query(db).ToQueryString().Split('\n').Select(l => l.Trim());
                Console.WriteLine($"      ok: {string.Join(" ", sql.SkipWhile(l => !l.StartsWith("WHERE", StringComparison.Ordinal)))}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"      {Describe(e)}");
                if (stack && provider == "Npgsql")
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}

if (postgres is not null)
{
    Console.WriteLine("######## The same on PostgreSQL, executed");
    using (var seed = new ShopContext("Npgsql", null, postgres))
    {
        seed.Database.EnsureDeleted();
        seed.Database.EnsureCreated();
        seed.Orders.Add(new Order { Code = new("FR76"), Codes = [new("FR76"), new("BE68")], Quantities = [new(1), new(3)], Tags = ["a", "x"], Labels = ["b"] });
        seed.Orders.Add(new Order { Code = new("BE68"), Codes = [new("BE68")], Quantities = [new(1)], Tags = ["a"], Labels = ["a"] });
        seed.SaveChanges();
    }

    foreach (var (queries, plugin) in new[] { (plain, (TranslationMode?)null), (withPlugin, TranslationMode.Column), (withPlugin, TranslationMode.Cast) })
    {
        foreach (var (name, query) in queries)
        {
            using var db = new ShopContext("Npgsql", plugin, postgres);
            try
            {
                Console.WriteLine($"   {name} [{plugin?.ToString() ?? "no plugin"}]: {query(db).Count()} row(s)");
            }
            catch (Exception e)
            {
                Console.WriteLine($"   {name} [{plugin?.ToString() ?? "no plugin"}]:\n      {Describe(e)}");
            }
        }
    }
}

static string Describe(Exception e)
{
    var text = $"!! {e.GetType().Name}: {e.Message.Split('\n')[0]}";
    for (var inner = e.InnerException; inner is not null; inner = inner.InnerException)
    {
        text += $"\n         inner {inner.GetType().Name}: {inner.Message.Split('\n')[0]}";
    }

    return text;
}

public readonly record struct Code(string Value);

public readonly record struct Quantity(short Value);

public sealed class CodeConverter() : ValueConverter<Code, string>(c => c.Value, v => new Code(v));

public sealed class QuantityConverter() : ValueConverter<Quantity, short>(q => q.Value, v => new Quantity(v));

public class Order
{
    public int Id { get; set; }
    public Code Code { get; set; }
    public List<Code> Codes { get; set; } = [];
    public List<Quantity> Quantities { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<string> Labels { get; set; } = [];
}

public enum TranslationMode { Column, Cast }

public class ShopContext(string provider, TranslationMode? plugin, string? postgres = null) : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        _ = provider switch
        {
            "Npgsql" => options.UseNpgsql(postgres ?? "Host=unused"),
            "SqlServer" => options.UseSqlServer("Server=unused"),
            _ => options.UseSqlite("Data Source=unused.db"),
        };

        if (plugin is { } mode)
        {
            ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new ValueQueriesExtension(mode));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Code).HasConversion<CodeConverter>().HasMaxLength(34);
            order.PrimitiveCollection(o => o.Codes).ElementType(e => e.HasConversion<CodeConverter>().HasMaxLength(34));
            order.PrimitiveCollection(o => o.Quantities).ElementType(e => e.HasConversion<QuantityConverter>());
            order.PrimitiveCollection(o => o.Labels).ElementType(e => e.HasMaxLength(10)); // character varying(10)[]
        });
}

/// <summary>Translates `x.Value`, where x is mapped through a value converter, to the column read as the underlying type.</summary>
public sealed class ValueTranslator(IRelationalTypeMappingSource typeMappingSource, ISqlExpressionFactory sql, TranslationMode mode)
    : IMemberTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (member.Name != "Value"
            || instance is not { TypeMapping: { Converter: not null } typeMapping }
            || typeMappingSource.FindMapping(returnType, typeMapping.StoreType) is not { } mapping)
        {
            return null;
        }

        return mode == TranslationMode.Column && instance is ColumnExpression column
            ? new ColumnExpression(column.Name, column.TableAlias, column.Column, returnType, mapping, column.IsNullable)
            : sql.Convert(instance, returnType, mapping);
    }
}

public sealed class ValueTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource, ISqlExpressionFactory sql, IDbContextOptions options)
    : IMemberTranslatorPlugin
{
    public IEnumerable<IMemberTranslator> Translators { get; } =
        [new ValueTranslator(typeMappingSource, sql, options.FindExtension<ValueQueriesExtension>()!.Mode)];
}

/// <summary>Registers the plugin, as any extension adding query services does.</summary>
public sealed class ValueQueriesExtension(TranslationMode mode) : IDbContextOptionsExtension
{
    public TranslationMode Mode { get; } = mode;

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
        => new EntityFrameworkRelationalServicesBuilder(services).TryAdd<IMemberTranslatorPlugin, ValueTranslatorPlugin>();

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(ValueQueriesExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => $"ValueQueries:{extension.Mode} ";

        public override int GetServiceProviderHashCode() => extension.Mode.GetHashCode();

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo info && ((ValueQueriesExtension)info.Extension).Mode == extension.Mode;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["ValueQueries"] = extension.Mode.ToString();
    }
}
