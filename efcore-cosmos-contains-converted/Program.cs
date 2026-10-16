using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// No Cosmos DB account is contacted: ToQueryString() only translates the query, and prints the parameters
// exactly as they are sent.
using var db = new BankContext();

string[] plains = ["FR76", "DE89"];
Show("1. string property, no converter: Contains", db.Accounts.Where(a => plains.Contains(a.Plain)));

Iban[] ibans = [new Iban("FR76"), new Iban("DE89")];
Show("2. Iban property, ValueConverter<Iban, string>: Contains", db.Accounts.Where(a => ibans.Contains(a.Iban)));
Show("3. Iban property, ValueConverter<Iban, string>: ==", db.Accounts.Where(a => a.Iban == ibans[0]));

Status[] statuses = [Status.Open, Status.Frozen];
Show("4. enum property, HasConversion<string>(): Contains", db.Accounts.Where(a => statuses.Contains(a.Status)));
Show("5. enum property, HasConversion<string>(): ==", db.Accounts.Where(a => a.Status == statuses[0]));

string[] currencies = ["eur", "usd"];
Show("6. string property, ValueConverter<string, string> (upper-cases): Contains", db.Accounts.Where(a => currencies.Contains(a.Currency)));
Show("7. string property, ValueConverter<string, string> (upper-cases): ==", db.Accounts.Where(a => a.Currency == currencies[0]));

static void Show(string label, IQueryable<Account> query)
{
    Console.WriteLine($"{label}:");
    try
    {
        Console.WriteLine(query.ToQueryString());
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception);
    }

    Console.WriteLine();
}

public readonly record struct Iban(string Value);

public enum Status { Open, Frozen, Closed }

public class Account
{
    public string Id { get; set; } = "";
    public string Plain { get; set; } = "";
    public Iban Iban { get; set; }
    public Status Status { get; set; }
    public string Currency { get; set; } = "";
}

public class BankContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseCosmos(
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            "Bank");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Account>(account =>
        {
            account.Property(a => a.Iban).HasConversion(new ValueConverter<Iban, string>(i => i.Value, s => new Iban(s)));
            account.Property(a => a.Status).HasConversion<string>();
            account.Property(a => a.Currency).HasConversion(new ValueConverter<string, string>(c => c.ToUpperInvariant(), c => c));
        });
}
