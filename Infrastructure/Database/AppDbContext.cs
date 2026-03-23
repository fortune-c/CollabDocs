using CollabDocs.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CollabDocs.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(d => d.Content)
                .IsRequired();

            entity.Property(d => d.CreatedAt)
                .IsRequired();

            entity.HasOne(d => d.Owner)
                .WithMany(u => u.OwnedDocuments)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentPermission>(entity =>
        {
            entity.HasKey(dp => dp.Id);

            entity.Property(dp => dp.Role)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(dp => dp.User)
                .WithMany(u => u.DocumentPermissions)
                .HasForeignKey(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dp => dp.Document)
                .WithMany(d => d.Permissions)
                .HasForeignKey(dp => dp.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(dp => new { dp.DocumentId, dp.UserId })
                .IsUnique();
        });
        
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(v => v.Id);
        
            entity.Property(v => v.Content)
                .IsRequired();
        
            entity.Property(v => v.VersionNumber)
                .IsRequired();
        
            entity.Property(v => v.CreatedAt)
                .IsRequired();
        
            entity.HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        
            entity.HasIndex(v => new { v.DocumentId, v.VersionNumber })
                .IsUnique();
        });
    }
}