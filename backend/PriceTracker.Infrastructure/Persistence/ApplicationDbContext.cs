namespace PriceTracker.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Domain.Entities;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User>                  Users                { get; set; }
    public DbSet<Category>              Categories           { get; set; }
    public DbSet<Currency>              Currencies           { get; set; }
    public DbSet<Store>                 Stores               { get; set; }
    public DbSet<AttributeType>         AttributeTypes       { get; set; }
    public DbSet<AttributeValue>        AttributeValues      { get; set; }
    public DbSet<Product>               Products             { get; set; }
    public DbSet<ProductImage>          ProductImages        { get; set; }
    public DbSet<ProductVariant>        ProductVariants      { get; set; }
    public DbSet<VariantAttribute>      VariantAttributes    { get; set; }
    public DbSet<StoreProductListing>   StoreProductListings { get; set; }
    public DbSet<PriceHistory>          PriceHistories       { get; set; }
    public DbSet<UserProductTracking>   UserProductTrackings { get; set; }
    public DbSet<Notification>          Notifications        { get; set; }
    public DbSet<ScrapeLog>             ScrapeLogs           { get; set; }
    public DbSet<RefreshToken>          RefreshTokens        { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}