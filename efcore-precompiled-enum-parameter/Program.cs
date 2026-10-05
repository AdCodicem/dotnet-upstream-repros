using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

File.Delete("repro.db");
using (var db = new BlogContext())
{
    db.Database.ExecuteSqlRaw("""CREATE TABLE "Blogs" ("Id" INTEGER NOT NULL PRIMARY KEY, "Status" TEXT NOT NULL)""");
    db.Database.ExecuteSqlRaw("""INSERT INTO "Blogs" VALUES (1, 'Published')""");
}
using (var db = new BlogContext())
{
    var status = Status.Published;
    var blogs = db.Blogs.Where(b => b.Status == status).ToList();
    Console.WriteLine($"Found {blogs.Count} blog(s)");
}

public enum Status { Draft, Published }

public class Blog
{
    public int Id { get; set; }
    public Status Status { get; set; }
}

public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=repro.db")
            .EnableSensitiveDataLogging()
            .LogTo(
                message => { if (message.Contains("SELECT")) Console.WriteLine(message); },
                [RelationalEventId.CommandExecuted], LogLevel.Information, DbContextLoggerOptions.None);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Blog>().Property(b => b.Status).HasConversion<string>(); // EnumToStringConverter<Status>
}
