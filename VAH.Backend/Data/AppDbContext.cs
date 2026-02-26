using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VAH.Backend.Models;

namespace VAH.Backend.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<Collection> Collections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Asset table configuration ──
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(a => a.Id);

            // FK: Asset → Collection
            entity.HasOne<Collection>()
                  .WithMany()
                  .HasForeignKey(a => a.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common query patterns
            entity.HasIndex(a => a.CollectionId);
            entity.HasIndex(a => a.ParentFolderId);
            entity.HasIndex(a => new { a.CollectionId, a.ParentFolderId })
                  .HasDatabaseName("IX_Assets_Collection_ParentFolder");
            entity.HasIndex(a => a.ContentType);
            entity.HasIndex(a => a.GroupId);
            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => a.IsFolder);

            // FK: Asset → User (nullable — system assets have no owner)
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(a => a.UserId);

            // Default values
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("datetime('now')");

            // Property constraints
            entity.Property(a => a.FileName).HasMaxLength(500);
            entity.Property(a => a.FilePath).HasMaxLength(2048);
            entity.Property(a => a.Tags).HasMaxLength(2000);
            entity.Property(a => a.ContentType).HasMaxLength(50);
        });

        // ── Collection table configuration ──
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(c => c.Id);

            // Self-referencing FK: Collection → Parent Collection
            entity.HasOne<Collection>()
                  .WithMany()
                  .HasForeignKey(c => c.ParentId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(c => c.ParentId);
            entity.HasIndex(c => c.Order);
            entity.HasIndex(c => c.Type);

            // FK: Collection → User (nullable — system collections have no owner)
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => c.UserId);

            // Default values
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");

            // Property constraints
            entity.Property(c => c.Name).HasMaxLength(255);
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.Color).HasMaxLength(20);
            entity.Property(c => c.Type).HasMaxLength(50);
            entity.Property(c => c.LayoutType).HasMaxLength(20);
        });

        // ── Seed default collections ──
        modelBuilder.Entity<Collection>().HasData(
            new Collection
            {
                Id = 1,
                Name = "Images",
                Description = "Lưu trữ hình ảnh",
                Type = "image",
                Color = "#007bff",
                Order = 1,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Collection
            {
                Id = 2,
                Name = "Links",
                Description = "Lưu trữ đường dẫn",
                Type = "link",
                Color = "#28a745",
                Order = 2,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Collection
            {
                Id = 3,
                Name = "Colors",
                Description = "Lưu trữ màu sắc",
                Type = "color",
                Color = "#ffc107",
                Order = 3,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}