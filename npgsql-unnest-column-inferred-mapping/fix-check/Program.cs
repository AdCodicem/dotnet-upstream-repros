// Tries the change proposed in the report: NpgsqlTypeMappingPostprocessor infers the type mapping of an unnest only
// when its array has no type mapping of its own (a parameter), as EF Core's SQL Server and SQLite postprocessors do.
// Every query is translated as shipped, then with a copy of the postprocessor carrying that condition, swapped in
// through IQueryTranslationPostprocessorFactory. Set REPRO_POSTGRES to a connection string to also run them.
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

var postgres = Environment.GetEnvironmentVariable("REPRO_POSTGRES");
var prefix = "FR";
short least = 2;
string[] prefixes = ["FR", "DE"];
string[] codes = ["BE68", "XX00"];

var queries = new (string Name, bool Plugin, string Expected, Func<ShopContext, IQueryable<Order>> Query)[]
{
    // Set operations over two array columns, text[] and character varying(10)[].
    ("o.Tags.Union(o.Labels).Contains(\"x\")", false, "1 row", db => db.Orders.Where(o => o.Tags.Union(o.Labels).Contains("x"))),
    ("o.Tags.Except(o.Labels).Any()", false, "1 row", db => db.Orders.Where(o => o.Tags.Except(o.Labels).Any())),
    ("o.Tags.Select(t => t + \"!\").Union(o.Labels.Select(l => l + \"!\")).Contains(\"x!\")", false, "1 row",
        db => db.Orders.Where(o => o.Tags.Select(t => t + "!").Union(o.Labels.Select(l => l + "!")).Contains("x!"))),

    // The member translator plugin: `.Value` of an element read as the column, under the underlying type.
    ("o.Codes.Any(c => c.Value.StartsWith(prefix))", true, "1 row", db => db.Orders.Where(o => o.Codes.Any(c => c.Value.StartsWith(prefix)))),
    ("o.Quantities.Any(q => q.Value > least)", true, "1 row", db => db.Orders.Where(o => o.Quantities.Any(q => q.Value > least))),

    // Arrays held in parameters: their mapping still has to be inferred.
    ("prefixes.Any(p => o.Code.Value.StartsWith(p)), a string[] parameter", true, "1 row", db => db.Orders.Where(o => prefixes.Any(p => o.Code.Value.StartsWith(p)))),
    ("codes.Contains(o.Code.Value), a string[] parameter", true, "1 row", db => db.Orders.Where(o => codes.Contains(o.Code.Value))),
    ("o.Tags.Intersect(prefixes).Any(), a column and a string[] parameter", false, "0 rows", db => db.Orders.Where(o => o.Tags.Intersect(prefixes).Any())),

    // Element store types PostgreSQL cannot unite: integer[] and text[] (a List<int> stored through a converter).
    ("o.Numbers.Union(o.TextNumbers).Contains(1), integer[] and text[]", false, "refused", db => db.Orders.Where(o => o.Numbers.Union(o.TextNumbers).Contains(1))),
};

if (postgres is not null)
{
    using var seed = new ShopContext(postgres, plugin: false, proposedChange: false);
    seed.Database.EnsureDeleted();
    seed.Database.EnsureCreated();
    seed.Orders.Add(new Order { Code = new("FR76"), Codes = [new("FR76"), new("BE68")], Quantities = [new(1), new(3)], Tags = ["a", "x"], Labels = ["b"], Numbers = [1, 2], TextNumbers = [3] });
    seed.Orders.Add(new Order { Code = new("BE68"), Codes = [new("BE68")], Quantities = [new(1)], Tags = ["a"], Labels = ["a"], Numbers = [1], TextNumbers = [1] });
    seed.SaveChanges();
}

foreach (var (name, plugin, expected, query) in queries)
{
    Console.WriteLine($"== {name}   (expected: {expected})");
    foreach (var proposedChange in new[] { false, true })
    {
        using var db = new ShopContext(postgres, plugin, proposedChange);
        var label = proposedChange ? "   with the proposed change: " : "   as shipped:               ";
        try
        {
            var sql = string.Join(" ", query(db).ToQueryString().Split('\n').Select(l => l.Trim()).SkipWhile(l => !l.StartsWith("WHERE", StringComparison.Ordinal)));
            Console.WriteLine(label + (postgres is null ? "translated" : $"{query(db).Count()} row(s)"));
            Console.WriteLine($"      {sql}");
        }
        catch (Exception e)
        {
            var innermost = e;
            while (innermost.InnerException is not null)
            {
                innermost = innermost.InnerException;
            }

            Console.WriteLine($"{label}!! {innermost.GetType().Name}: {innermost.Message.Split('\n')[0].Trim()}");
        }
    }
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
    public int[] Numbers { get; set; } = [];
    public List<int> TextNumbers { get; set; } = [];
}

public class ShopContext(string? postgres, bool plugin, bool proposedChange) : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(postgres ?? "Host=unused");
        if (proposedChange)
        {
            options.ReplaceService<IQueryTranslationPostprocessorFactory, ProposedQueryTranslationPostprocessorFactory>();
        }

        if (plugin)
        {
            ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new ValueQueriesExtension());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Code).HasConversion<CodeConverter>().HasMaxLength(34);
            order.PrimitiveCollection(o => o.Codes).ElementType(e => e.HasConversion<CodeConverter>().HasMaxLength(34));
            order.PrimitiveCollection(o => o.Quantities).ElementType(e => e.HasConversion<QuantityConverter>());
            order.PrimitiveCollection(o => o.Labels).ElementType(e => e.HasMaxLength(10)); // character varying(10)[]
            order.PrimitiveCollection(o => o.TextNumbers).ElementType(e => e.HasConversion<string>()); // text[]
        });
}

/// <summary>NpgsqlTypeMappingPostprocessor as of 10.0.3 and 11.0.0-rc.1.1, with one condition added.</summary>
public class ProposedTypeMappingPostprocessor(
    QueryTranslationPostprocessorDependencies dependencies,
    RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
    RelationalQueryCompilationContext queryCompilationContext)
    : RelationalTypeMappingPostprocessor(dependencies, relationalDependencies, queryCompilationContext)
{
    private readonly IModel _model = queryCompilationContext.Model;
    private readonly IRelationalTypeMappingSource _typeMappingSource = relationalDependencies.TypeMappingSource;
    private readonly ISqlExpressionFactory _sqlExpressionFactory = relationalDependencies.SqlExpressionFactory;

    protected override Expression VisitExtension(Expression expression)
    {
        switch (expression)
        {
            case PgUnnestExpression unnestExpression
                when unnestExpression.Array.TypeMapping is null // the proposed change
                    && TryGetInferredTypeMapping(unnestExpression.Alias, unnestExpression.ColumnName, out var elementTypeMapping):
            {
                var collectionTypeMapping = _typeMappingSource.FindMapping(unnestExpression.Array.Type, _model, elementTypeMapping);

                if (collectionTypeMapping is null)
                {
                    throw new InvalidOperationException(RelationalStrings.NullTypeMappingInSqlTree(expression.ToString()));
                }

                return unnestExpression.Update(
                    _sqlExpressionFactory.ApplyTypeMapping(unnestExpression.Array, collectionTypeMapping));
            }

            default:
                return base.VisitExtension(expression);
        }
    }
}

public class ProposedQueryTranslationPostprocessor(
    QueryTranslationPostprocessorDependencies dependencies,
    RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
    RelationalQueryCompilationContext queryCompilationContext)
    : NpgsqlQueryTranslationPostprocessor(dependencies, relationalDependencies, queryCompilationContext)
{
    protected override Expression ProcessTypeMappings(Expression expression)
        => new ProposedTypeMappingPostprocessor(Dependencies, RelationalDependencies, RelationalQueryCompilationContext).Process(expression);
}

public class ProposedQueryTranslationPostprocessorFactory(
    QueryTranslationPostprocessorDependencies dependencies,
    RelationalQueryTranslationPostprocessorDependencies relationalDependencies)
    : IQueryTranslationPostprocessorFactory
{
    public QueryTranslationPostprocessor Create(QueryCompilationContext queryCompilationContext)
        => new ProposedQueryTranslationPostprocessor(dependencies, relationalDependencies, (RelationalQueryCompilationContext)queryCompilationContext);
}

/// <summary>The plugin of the repro, in its "column re-typed" form.</summary>
public sealed class ValueTranslator(IRelationalTypeMappingSource typeMappingSource) : IMemberTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        => member.Name == "Value"
            && instance is ColumnExpression { TypeMapping: { Converter: not null } typeMapping } column
            && typeMappingSource.FindMapping(returnType, typeMapping.StoreType) is { } mapping
                ? new ColumnExpression(column.Name, column.TableAlias, column.Column, returnType, mapping, column.IsNullable)
                : null;
}

public sealed class ValueTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource) : IMemberTranslatorPlugin
{
    public IEnumerable<IMemberTranslator> Translators { get; } = [new ValueTranslator(typeMappingSource)];
}

public sealed class ValueQueriesExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
        => new EntityFrameworkRelationalServicesBuilder(services).TryAdd<IMemberTranslatorPlugin, ValueTranslatorPlugin>();

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(ValueQueriesExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "ValueQueries ";

        public override int GetServiceProviderHashCode() => 1;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo["ValueQueries"] = "1";
    }
}
