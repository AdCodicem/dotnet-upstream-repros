using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

File.Delete("repro.db");
using (var db = new BlogContext())
{
    db.Database.ExecuteSqlRaw("""CREATE TABLE "Blogs" ("Id" INTEGER NOT NULL PRIMARY KEY, "Status" TEXT NOT NULL)""");
    db.Database.ExecuteSqlRaw("""CREATE TABLE "Posts" ("Id" INTEGER NOT NULL PRIMARY KEY, "Rating" INTEGER NOT NULL)""");
    db.Database.ExecuteSqlRaw("""INSERT INTO "Blogs" VALUES (1, 'Published')""");
    db.Database.ExecuteSqlRaw("""INSERT INTO "Posts" VALUES (1, 5)""");
}

using (var db = new BlogContext())
{
    try
    {
        var blog = db.Blogs.ToList()[0];
        Console.WriteLine($"Blog {blog.Id}: {blog.Status}");
    }
    catch (Exception e) { Console.WriteLine(e); }

    try
    {
        var post = db.Posts.ToList()[0];
        Console.WriteLine($"Post {post.Id}: rated {post.Rating.Value}");
    }
    catch (Exception e) { Console.WriteLine(e); }
}

public enum Status { Draft, Published }

public readonly record struct Rating(int Value);

public sealed class RatingConverter() : ValueConverter<Rating, int>(r => r.Value, value => new Rating(value));

public class Blog
{
    public int Id { get; set; }
    public Status Status { get; set; }
}

public class Post
{
    public int Id { get; set; }
    public Rating Rating { get; set; }
}

public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite("Data Source=repro.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>().Property(b => b.Status).HasConversion<string>();          // EnumToStringConverter<Status>
        modelBuilder.Entity<Post>().Property(p => p.Rating).HasConversion<RatingConverter>(); // a ValueConverter subclass
        // Fails the same way: .HasConversion(new RatingConverter()), or, in ConfigureConventions,
        // configurationBuilder.Properties<Rating>().HaveConversion<RatingConverter>().
    }
}
