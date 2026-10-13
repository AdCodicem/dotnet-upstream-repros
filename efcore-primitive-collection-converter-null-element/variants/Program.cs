// The same failure in other shapes. Each one runs on a database of its own.
//   dotnet run                        plain types, convertsNulls, a collection parameter in the default mode
//   dotnet run -- --parameter-mode    the collection parameter sent as one JSON parameter
// Set REPRO_SQLSERVER to a connection string (without Database=) to also write through the SQL Server provider.
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

var parameterMode = args.Contains("--parameter-mode");
var sqlServer = Environment.GetEnvironmentVariable("REPRO_SQLSERVER");

if (!parameterMode)
{
    Run("A. write List<int?> = [5, 0], element converter ValueConverter<int?, int?> mapping 0 to null", db =>
    {
        db.Add(new Order { Plain = [5, 0] });
        db.SaveChanges();
        return "stored Plain=" + Scalar(db, """SELECT "Plain" FROM "Orders" """);
    });
    Run("B. read List<int?> from [5,0], the same converter", db =>
    {
        db.Database.ExecuteSqlRaw("""INSERT INTO "Orders" ("Id", "Single", "Plain", "Lines", "Flagged") VALUES (100, NULL, '[5,0]', '[]', '[]')""");
        return "Plain=[" + string.Join(", ", db.Orders.Single(o => o.Id == 100).Plain.Select(l => l?.ToString() ?? "null")) + "]";
    });
    Run("C. write List<Quantity?> = [5, Quantity(0)], the converter built with convertsNulls: true", db =>
    {
        db.Add(new Order { Flagged = [new Quantity(5), new Quantity(0)] });
        db.SaveChanges();
        return "stored Flagged=" + Scalar(db, """SELECT "Flagged" FROM "Orders" """);
    });
}

Run($"D. wanted.Contains(o.Single), wanted = [Quantity(0)], {(parameterMode ? "ParameterTranslationMode.Parameter" : "the default mode")}", db =>
{
    List<Quantity?> wanted = [new Quantity(0)];
    return db.Orders.Count(o => wanted.Contains(o.Single)) + " row(s)";
});

if (!parameterMode && sqlServer is not null)
{
    using var db = new ShopContext(options => options.UseSqlServer($"{sqlServer};Database=ReproNullElement", sql => sql.EnableRetryOnFailure()));
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
    db.Database.OpenConnection();
    Try($"E. SQL Server {db.Database.GetDbConnection().ServerVersion}: write List<Quantity?> = [5, Quantity(0)]", db, context =>
    {
        context.Add(new Order { Lines = [new Quantity(5), new Quantity(0)] });
        context.SaveChanges();
        return "stored Lines=" + Scalar(context, "SELECT [Lines] FROM [Orders]");
    });
    db.Database.CloseConnection();
    db.Database.EnsureDeleted();
}

void Run(string label, Func<ShopContext, string> action)
{
    using var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    using var db = new ShopContext(options => options.UseSqlite(connection, sqlite =>
    {
        if (parameterMode)
        {
            sqlite.UseParameterizedCollectionMode(ParameterTranslationMode.Parameter);
        }
    }));
    db.Database.EnsureCreated();
    Try(label, db, action);
}

static void Try(string label, ShopContext db, Func<ShopContext, string> action)
{
    try
    {
        Console.WriteLine($"{label}: {action(db)}");
    }
    catch (Exception exception)
    {
        var inner = exception is DbUpdateException ? exception.InnerException! : exception;
        Console.WriteLine($"{label}: {inner.GetType().Name}: {inner.Message}");
        foreach (var frame in inner.StackTrace!.Split('\n').Take(2))
        {
            Console.WriteLine("   " + frame.Trim());
        }
    }
}

static string Scalar(ShopContext db, string sql)
{
    var connection = db.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
    }

    using var command = connection.CreateCommand();
    command.CommandText = sql;
    return (string)command.ExecuteScalar()!;
}

public readonly record struct Quantity(int Value);

public sealed class QuantityConverter() : ValueConverter<Quantity?, int?>(
    quantity => quantity == null || quantity.Value.Value == 0 ? null : quantity.Value.Value,
    value => value == null || value == 0 ? null : new Quantity(value.Value));

public sealed class ConvertsNullsQuantityConverter() : ValueConverter<Quantity?, int?>(
    quantity => quantity == null || quantity.Value.Value == 0 ? null : quantity.Value.Value,
    value => value == null || value == 0 ? null : new Quantity(value.Value),
    convertsNulls: true);

public sealed class ZeroToNullConverter() : ValueConverter<int?, int?>(
    value => value == 0 ? null : value,
    value => value == 0 ? null : value);

public class Order
{
    public int Id { get; set; }
    public Quantity? Single { get; set; }
    public List<int?> Plain { get; set; } = [];
    public List<Quantity?> Lines { get; set; } = [];
    public List<Quantity?> Flagged { get; set; } = [];
}

public class ShopContext(Action<DbContextOptionsBuilder> configure) : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) => configure(options);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Single).HasConversion<QuantityConverter>();
            order.PrimitiveCollection(o => o.Plain).ElementType().HasConversion<ZeroToNullConverter>();
            order.PrimitiveCollection(o => o.Lines).ElementType().HasConversion<QuantityConverter>();
            order.PrimitiveCollection(o => o.Flagged).ElementType().HasConversion<ConvertsNullsQuantityConverter>();
        });
}
