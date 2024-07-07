using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.DbContext;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<RefreshToken> RefreshTokens { get; init; }
    public DbSet<Item> Items { get; init; }
    public DbSet<Inventory> Inventory { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FirstName).HasColumnName("firstname").IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("lastname").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).HasColumnName("password").IsRequired().HasMaxLength(255);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Define the one-to-many relationship with Inventory
            entity.HasMany(e => e.Inventory)
                .WithOne(i => i.User)
                .HasForeignKey(i => i.UserId);
        });

        // User refresh token configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.Expiration).IsRequired();

            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Item configuration
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Define the one-to-many relationship with Inventory
            entity.HasMany(i => i.Inventory)
                .WithOne(i => i.Item)
                .HasForeignKey(i => i.ItemId)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade delete to remove inventory if the item is deleted
        });

        // Inventory configuration
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("inventory");

            entity.HasKey(e => new { e.UserId, e.ItemId });

            entity.Property(e => e.Quantity)
                .IsRequired()
                .HasDefaultValue(0);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Inventory)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade delete to remove inventory if the user is deleted

            entity.HasOne(e => e.Item)
                .WithMany(i => i.Inventory)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade delete to remove inventory if the item is deleted
        });
    }
}