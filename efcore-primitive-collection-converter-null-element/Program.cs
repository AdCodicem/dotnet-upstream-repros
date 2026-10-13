using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
using (var db = new ShopContext(connection))
{
    db.Database.EnsureCreated();
}

// Writes: the converter maps Quantity(0) to null, and Code("") to null.
Save("1. scalar Quantity? = Quantity(0)", new Order { Single = new Quantity(0) });
Save("2. List<Quantity?> = [5, null]", new Order { Lines = [new Quantity(5), null] });
Save("3. List<Quantity?> = [5, Quantity(0)]", new Order { Lines = [new Quantity(5), new Quantity(0)] });
Save("4. List<Code?> = [\"A\", Code(\"\")]", new Order { Codes = [new Code("A"), new Code("")] });

// Reads: the converter maps 0 to null; the rows below are written by another program.
Execute("""INSERT INTO "Orders" ("Id", "Single", "Lines", "Codes") VALUES (100, 0, '[]', '[]')""");
Execute("""INSERT INTO "Orders" ("Id", "Single", "Lines", "Codes") VALUES (101, NULL, '[5,0]', '[]')""");
Read("5. read scalar Single = 0", 100);
Read("6. read Lines = [5,0]", 101);

void Save(string label, Order order)
{
    using var db = new ShopContext(connection);
    db.Add(order);
    try
    {
        db.SaveChanges();
        using var command = connection.CreateCommand();
        command.CommandText = $"""SELECT quote("Single"), "Lines", "Codes" FROM "Orders" WHERE "Id" = {order.Id}""";
        using var reader = command.ExecuteReader();
        reader.Read();
        Console.WriteLine($"{label}: stored Single={reader.GetString(0)} Lines={reader.GetString(1)} Codes={reader.GetString(2)}");
    }
    catch (DbUpdateException exception)
    {
        Console.WriteLine($"{label}: {exception.GetType().Name}");
        Console.WriteLine(exception.InnerException);
    }
}

void Read(string label, int id)
{
    using var db = new ShopContext(connection);
    try
    {
        var order = db.Orders.Single(o => o.Id == id);
        Console.WriteLine($"{label}: Single={order.Single?.ToString() ?? "null"} Lines=[{string.Join(", ", order.Lines.Select(l => l?.ToString() ?? "null"))}]");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"{label}:");
        Console.WriteLine(exception);
    }
}

void Execute(string sql)
{
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.ExecuteNonQuery();
}

/// <summary>A quantity; Quantity(0) means "no quantity" and is stored as null.</summary>
public readonly record struct Quantity(int Value);

/// <summary>A code over text; Code("") means "no code" and is stored as null.</summary>
public readonly record struct Code(string Value);

public sealed class QuantityConverter() : ValueConverter<Quantity?, int?>(
    quantity => quantity == null || quantity.Value.Value == 0 ? null : quantity.Value.Value,
    value => value == null || value == 0 ? null : new Quantity(value.Value));

public sealed class CodeConverter() : ValueConverter<Code?, string?>(
    code => code == null || code.Value.Value == "" ? null : code.Value.Value,
    value => value == null || value == "" ? null : new Code(value));

public class Order
{
    public int Id { get; set; }
    public Quantity? Single { get; set; }
    public List<Quantity?> Lines { get; set; } = [];
    public List<Code?> Codes { get; set; } = [];
}

public class ShopContext(SqliteConnection connection) : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Single).HasConversion<QuantityConverter>();
            order.PrimitiveCollection(o => o.Lines).ElementType().HasConversion<QuantityConverter>();
            order.PrimitiveCollection(o => o.Codes).ElementType().HasConversion<CodeConverter>();
        });
}
