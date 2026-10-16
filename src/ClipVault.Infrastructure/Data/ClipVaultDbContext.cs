using System.Globalization;
using ClipVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipVault.Infrastructure.Data;

public sealed class ClipVaultDbContext(DbContextOptions<ClipVaultDbContext> options)
    : DbContext(options)
{
    public DbSet<ClipItem> Clips => Set<ClipItem>();

    public DbSet<CategoryItem> Categories => Set<CategoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var clip = modelBuilder.Entity<ClipItem>();

        clip.ToTable("Clips");
        clip.HasKey(x => x.Id);

        clip.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        clip.Property(x => x.Type)
            .HasConversion<int>()
            .HasColumnType("INTEGER")
            .IsRequired();

        clip.Property(x => x.Content).HasColumnType("TEXT");

        // El arreglo de bytes se persiste directamente en SQLite como BLOB.
        clip.Property(x => x.ImageData).HasColumnType("BLOB");

        clip.Property(x => x.Title).HasColumnType("TEXT");
        clip.Property(x => x.Category).HasColumnType("TEXT");

        clip.Property(x => x.IsPermanent)
            .HasColumnType("INTEGER")
            .HasDefaultValue(false)
            .IsRequired();

        // SQLite no tiene DateTime nativo: se guarda ISO-8601 UTC en TEXT.
        clip.Property(x => x.CreatedAt)
            .HasColumnType("TEXT")
            .HasConversion(
                value => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                value => DateTime.Parse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind))
            .IsRequired();

        clip.HasIndex(x => x.CreatedAt);
        clip.HasIndex(x => x.IsPermanent);

        var category = modelBuilder.Entity<CategoryItem>();
        category.ToTable("Categories");
        category.HasKey(x => x.Name);
        category.Property(x => x.Name)
            .HasColumnType("TEXT")
            .IsRequired();
        category.Property(x => x.IsUserCreated)
            .HasColumnType("INTEGER")
            .HasDefaultValue(false)
            .IsRequired();
    }
}
