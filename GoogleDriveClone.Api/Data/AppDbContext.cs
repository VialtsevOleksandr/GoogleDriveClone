using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GoogleDriveClone.Api.Entities;

namespace GoogleDriveClone.Api.Data;

public class AppDbContext : IdentityDbContext<User>
{
    public DbSet<FileMetadata> Files { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalName).IsRequired();
            entity.Property(e => e.Size).IsRequired();
            entity.Property(e => e.ContentType).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.FileHash).IsRequired();
            entity.Property(e => e.ModifiedAt).IsRequired(false);

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Files)
                .HasForeignKey(e => e.OwnerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
