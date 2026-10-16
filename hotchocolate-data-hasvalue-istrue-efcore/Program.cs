// FilterExpressionBuilder.HasValue (and so HasValueAndAlso) builds `Expression.IsTrue(x.HasValue)`, a UnaryExpression
// of node type IsTrue, which EF Core does not translate. The builder's other null guards, NotNull and NotNullAndAlso,
// write `x != null`, which EF Core translates to IS NOT NULL. FilterExpressionBuilder is public, and is what a custom
// operation handler (QueryableOperationHandlerBase) is expected to build its expression with.
using System.Linq.Expressions;
using HotChocolate.Data.Filters.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
var options = new DbContextOptionsBuilder<ShopContext>().UseSqlite(connection).Options;
using (var seed = new ShopContext(options))
{
    seed.Database.EnsureCreated();
    seed.Orders.AddRange(new Order { Id = 1, Quantity = 3 }, new Order { Id = 2, Quantity = null }, new Order { Id = 3, Quantity = 7 });
    seed.SaveChanges();
}

var order = Expression.Parameter(typeof(Order), "o");
var quantity = Expression.Property(order, nameof(Order.Quantity));                      // int?
var greaterThanFive = Expression.GreaterThan(                                            // o.Quantity.Value > 5
    Expression.Property(quantity, nameof(Nullable<int>.Value)), Expression.Constant(5));

var cases = new (string Name, Expression Body)[]
{
    ("FilterExpressionBuilder.HasValueAndAlso(o.Quantity, o.Quantity.Value > 5)", FilterExpressionBuilder.HasValueAndAlso(quantity, greaterThanFive)),
    ("FilterExpressionBuilder.HasValue(o.Quantity)", FilterExpressionBuilder.HasValue(quantity)),
    ("FilterExpressionBuilder.NotNullAndAlso(o.Quantity, o.Quantity.Value > 5)", FilterExpressionBuilder.NotNullAndAlso(quantity, greaterThanFive)),
    ("Expression.AndAlso(o.Quantity.HasValue, o.Quantity.Value > 5), no IsTrue", Expression.AndAlso(Expression.Property(quantity, nameof(Nullable<int>.HasValue)), greaterThanFive)),
};

foreach (var (name, body) in cases)
{
    var predicate = Expression.Lambda<Func<Order, bool>>(body, order);
    Console.WriteLine($"== {name}");
    Console.WriteLine($"   expression: {predicate}");
    Console.WriteLine($"   in memory:  [{string.Join(", ", Orders().AsQueryable().Where(predicate).Select(o => o.Id))}]");
    using var db = new ShopContext(options);
    try
    {
        var ids = db.Orders.Where(predicate).OrderBy(o => o.Id).Select(o => o.Id).ToList();
        Console.WriteLine($"   EF Core:    [{string.Join(", ", ids)}]");
        Console.WriteLine($"   SQL:        {db.Orders.Where(predicate).ToQueryString().Split('\n').Last().Trim()}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"   EF Core:    !! {exception.GetType().Name}: {exception.Message.Split('\n')[0].Trim()}");
        Console.WriteLine($"               {string.Join(" ", exception.Message.Split('\n').Skip(1).Select(l => l.Trim())).Trim()}");
    }
    Console.WriteLine();
}

static Order[] Orders() => [new() { Id = 1, Quantity = 3 }, new() { Id = 2, Quantity = null }, new() { Id = 3, Quantity = 7 }];

public class Order
{
    public int Id { get; set; }

    public int? Quantity { get; set; }
}

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
