using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
using (var command = connection.CreateCommand())
{
    command.CommandText = """
        CREATE TABLE "Readings" ("Id" INTEGER NOT NULL PRIMARY KEY, "Value" INTEGER NOT NULL);
        INSERT INTO "Readings" VALUES (1, 5), (2, -9223372036854775808);
        """;
    command.ExecuteNonQuery();
}

// SQLite raises "integer overflow" for abs(long.MinValue). Microsoft.Data.Sqlite steps to the first row inside
// ExecuteReader, so the row the error is raised on decides where it surfaces.
Run("1. The failing row is the first one (raised by ExecuteReader)", db => db.Readings.OrderByDescending(r => r.Id).Select(r => Math.Abs(r.Value)).ToList());
Run("2. The failing row is the second one (raised by DbDataReader.Read)", db => db.Readings.OrderBy(r => r.Id).Select(r => Math.Abs(r.Value)).ToList());

void Run(string label, Func<MeterContext, List<long>> query)
{
    Console.WriteLine($"{label}:");
    using var db = new MeterContext(connection);
    try
    {
        query(db);
        Console.WriteLine("   no exception");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"   caught by the caller: {exception.GetType().Name}: {exception.Message}");
    }

    Console.WriteLine();
}

public class Reading
{
    public int Id { get; set; }
    public long Value { get; set; }
}

public class MeterContext(SqliteConnection connection) : DbContext
{
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .UseSqlite(connection)
            .AddInterceptors(new ErrorInterceptor())
            .LogTo(
                (eventId, _) => eventId == CoreEventId.QueryIterationFailed || eventId == RelationalEventId.CommandError,
                eventData => Console.WriteLine($"   logged: {eventData.EventId.Name}"));
}

public class ErrorInterceptor : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Console.WriteLine("   interceptor: ReaderExecuted");
        return result;
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        => Console.WriteLine($"   interceptor: CommandFailed ({eventData.Exception.GetType().Name}: {eventData.Exception.Message})");

    public override InterceptionResult DataReaderClosing(DbCommand command, DataReaderClosingEventData eventData, InterceptionResult result)
    {
        Console.WriteLine("   interceptor: DataReaderClosing");
        return result;
    }

    public override InterceptionResult DataReaderDisposing(DbCommand command, DataReaderDisposingEventData eventData, InterceptionResult result)
    {
        Console.WriteLine("   interceptor: DataReaderDisposing");
        return result;
    }
}
