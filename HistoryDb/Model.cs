public class HistoryItem
{
    public int Id { get; set; }
    public required string Type { get; set; }
    public required DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ContentId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public class HistoryDbContext(DbContextOptions<HistoryDbContext> options) : DbContext(options)
{
    private static readonly Func<HistoryDbContext, int?, int?, int?, int, IAsyncEnumerable<HistoryItem>> itemsQuery =
        EF.CompileAsyncQuery((HistoryDbContext context, int? catalogBrandId, int? before, int? after, int pageSize) =>
           context.HistoryItems.AsNoTracking()
                  .OrderBy(ci => ci.Id)
                  .Where(ci => before == null || ci.Id <= before)
                  .Where(ci => after == null || ci.Id >= after)
                  .Take(pageSize + 1));

    public Task<List<HistoryItem>> GetHistoryItemsCompiledAsync(int? catalogBrandId, int? before, int? after, int pageSize)
    {
        return ToListAsync(itemsQuery(this, catalogBrandId, before, after, pageSize));
    }

    public DbSet<HistoryItem> HistoryItems => Set<HistoryItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        DefineHistoryItem(builder.Entity<HistoryItem>());
    }

    private static void DefineHistoryItem(EntityTypeBuilder<HistoryItem> builder)
    {
        builder.ToTable("History");

        builder.HasKey(c => c.Id);

        builder.Property(ci => ci.Id)
            .UseHiLo("history_hilo")
            .IsRequired();

        builder.Property(cb => cb.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cb => cb.Timestamp)
            .IsRequired();

        builder.Property(cb => cb.ContentId)
            .IsRequired(false)
            .HasMaxLength(1024);

        builder.Property(cb => cb.SourceUrl)
            .IsRequired(false)
            .HasMaxLength(1024);

        builder.Property(cb => cb.Description)
            .IsRequired(false)
            .HasMaxLength(2048);
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        var results = new List<T>();
        await foreach (var value in asyncEnumerable)
        {
            results.Add(value);
        }

        return results;
    }
}
