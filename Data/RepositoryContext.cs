using Microsoft.EntityFrameworkCore;
using MavenRepositoryServer.Models;

namespace MavenRepositoryServer.Data;

public class RepositoryContext : DbContext
{
    public RepositoryContext(DbContextOptions<RepositoryContext> options)
        : base(options)
    {
    }

    public DbSet<Artifact> Artifacts { get; set; }
    public DbSet<ArtifactFile> ArtifactFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Artifact>()
            .HasIndex(a => new { a.GroupId, a.ArtifactId, a.Version })
            .IsUnique();

        modelBuilder.Entity<ArtifactFile>()
            .HasOne(af => af.Artifact)
            .WithMany(a => a.Files)
            .HasForeignKey(af => af.ArtifactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 