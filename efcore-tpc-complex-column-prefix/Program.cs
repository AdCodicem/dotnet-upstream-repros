using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

// Where does each property of the complex types go, per mapping strategy?
Columns<TphContext>();
Columns<TptContext>();
Columns<TpcContext>();
Columns<TpcConcreteRootContext>();
Columns<TpcTwoAddressesContext>();
Columns<TpcNestedContext>();
Columns<TpcExplicitNamesContext>(); // the workaround: column names set on the root

// What a migration does when a second complex property of the same type is added to the TPC root.
BaseContext.Connection.Open();
using (var before = new TpcContext())
{
    before.Database.EnsureCreated();
    before.Add(new Ferry { Id = 1, Cars = 12, Shipping = new Address { Zip = "75001" } });
    before.SaveChanges();
    Console.WriteLine("===== TpcContext: saved Ferry 1 with Shipping.Zip = 75001");
}

using (var before = new TpcContext())
using (var after = new TpcTwoAddressesContext())
{
    Console.WriteLine("===== migration from TpcContext to TpcTwoAddressesContext (Billing added)");
    var target = after.GetService<IDesignTimeModel>().Model;
    var operations = after.GetService<IMigrationsModelDiffer>().GetDifferences(
        before.GetService<IDesignTimeModel>().Model.GetRelationalModel(),
        target.GetRelationalModel());
    foreach (var command in after.GetService<IMigrationsSqlGenerator>().Generate(operations, target))
    {
        Console.WriteLine("  " + command.CommandText.Trim());
        after.Database.ExecuteSqlRaw(command.CommandText);
    }
}

using (var after = new TpcTwoAddressesContext())
{
    var ferry = after.Set<Ferry>().Single();
    Console.WriteLine($"===== TpcTwoAddressesContext: read Ferry 1 with Shipping.Zip = '{ferry.Shipping.Zip}', Billing.Zip = '{ferry.Billing.Zip}'");
}

static void Columns<TContext>()
    where TContext : DbContext, new()
{
    using var db = new TContext();
    Console.WriteLine($"===== {typeof(TContext).Name}");
    foreach (var table in db.GetService<IDesignTimeModel>().Model.GetRelationalModel().Tables)
    {
        Console.WriteLine($"  {table.Name}: " + string.Join(", ", table.Columns.Select(column =>
            column.Name + " <- " + string.Join(" + ", column.PropertyMappings.Select(m => Path(m.Property)).Distinct()))));
    }
}

static string Path(IProperty property)
{
    var path = property.Name;
    var type = property.DeclaringType;
    while (type is IComplexType complexType)
    {
        path = complexType.ComplexProperty.Name + "." + path;
        type = complexType.ComplexProperty.DeclaringType;
    }

    return path;
}

public class Address
{
    public string Zip { get; set; } = "";
}

public class Location
{
    public string Zip { get; set; } = "";
    public Address Address { get; set; } = new();
}

public abstract class Vessel
{
    public int Id { get; set; }
    public Address Shipping { get; set; } = new();
    public Address Billing { get; set; } = new();
    public Location Port { get; set; } = new();
}

public class Ferry : Vessel
{
    public int Cars { get; set; }
}

public class Ship
{
    public int Id { get; set; }
    public Address Shipping { get; set; } = new();
}

public class Tanker : Ship
{
    public int Tons { get; set; }
}

public abstract class BaseContext : DbContext
{
    public static readonly SqliteConnection Connection = new("Data Source=:memory:");

    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(Connection);
}

public class TphContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vessel>(v => { v.UseTphMappingStrategy(); v.ComplexProperty(x => x.Shipping); v.Ignore(x => x.Billing); v.Ignore(x => x.Port); });
        modelBuilder.Entity<Ferry>();
    }
}

public class TptContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vessel>(v => { v.UseTptMappingStrategy(); v.ComplexProperty(x => x.Shipping); v.Ignore(x => x.Billing); v.Ignore(x => x.Port); });
        modelBuilder.Entity<Ferry>().ToTable("Ferries");
    }
}

public class TpcContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vessel>(v => { v.UseTpcMappingStrategy(); v.ComplexProperty(x => x.Shipping); v.Ignore(x => x.Billing); v.Ignore(x => x.Port); });
        modelBuilder.Entity<Ferry>().ToTable("Ferries");
    }
}

public class TpcConcreteRootContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ship>(s => { s.UseTpcMappingStrategy(); s.ToTable("Ships"); s.ComplexProperty(x => x.Shipping); });
        modelBuilder.Entity<Tanker>().ToTable("Tankers");
    }
}

public class TpcTwoAddressesContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vessel>(v => { v.UseTpcMappingStrategy(); v.ComplexProperty(x => x.Shipping); v.ComplexProperty(x => x.Billing); v.Ignore(x => x.Port); });
        modelBuilder.Entity<Ferry>().ToTable("Ferries");
    }
}

public class TpcNestedContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vessel>(v => { v.UseTpcMappingStrategy(); v.Ignore(x => x.Shipping); v.Ignore(x => x.Billing); v.ComplexProperty(x => x.Port, p => p.ComplexProperty(x => x.Address)); });
        modelBuilder.Entity<Ferry>().ToTable("Ferries");
    }
}

public class TpcExplicitNamesContext : BaseContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vessel>(v =>
        {
            v.UseTpcMappingStrategy();
            v.ComplexProperty(x => x.Shipping, c => c.Property(a => a.Zip).HasColumnName("Shipping_Zip"));
            v.ComplexProperty(x => x.Billing, c => c.Property(a => a.Zip).HasColumnName("Billing_Zip"));
            v.Ignore(x => x.Port);
        });
        modelBuilder.Entity<Ferry>().ToTable("Ferries");
    }
}
