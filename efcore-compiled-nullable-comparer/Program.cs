using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using (var db = new ShopContext())
{
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
    db.Add(new Order { Quantity = new Qty(3), Backordered = new Qty(2) });
    db.Add(new Order { Quantity = new Qty(1) });
    db.SaveChanges();
}

using (var db = new ShopContext())
{
    var compiled = db.Model.GetType().Namespace == "Compiled";
    Console.WriteLine($"Model: {(compiled ? "compiled" : "built at run time")}");
    foreach (var order in db.Orders.OrderBy(o => o.Id))
    {
        Console.WriteLine($"Order {order.Id}: Quantity={order.Quantity.Value} Backordered={order.Backordered?.Value.ToString() ?? "null"}");
    }

    var comparer = db.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.Backordered))!.GetValueComparer();
    Console.WriteLine($"Backordered's comparer: {comparer.GetType().Name}");
}

public readonly record struct Qty(int Value);

public sealed class QtyConverter() : ValueConverter<Qty, int>(q => q.Value, v => new Qty(v));

public sealed class QtyComparer() : ValueComparer<Qty>((a, b) => a.Value == b.Value, q => q.Value.GetHashCode());

#if WORKAROUND
public sealed class NullableQtyComparer() : ValueComparer<Qty?>(
    (a, b) => a.HasValue ? b.HasValue && a.Value.Value == b.Value.Value : !b.HasValue,
    q => q.HasValue ? q.Value.Value.GetHashCode() : 0);
#endif

public class Order
{
    public int Id { get; set; }
    public Qty Quantity { get; set; }
    public Qty? Backordered { get; set; }
}

public class ShopContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=repro.db");

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // The configuration applies to Qty and to Qty?.
        configurationBuilder.Properties<Qty>().HaveConversion<QtyConverter, QtyComparer>();
#if WORKAROUND
        configurationBuilder.Properties<Qty?>().HaveConversion<QtyConverter, NullableQtyComparer>();
#endif
    }
}
