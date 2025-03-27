using System.ComponentModel.DataAnnotations;

namespace MavenRepositoryServer.Models;

public class ArtifactFile
{
    [Key]
    public long Id { get; set; }
    
    public long ArtifactId { get; set; }
    public Artifact Artifact { get; set; } = null!;
    
    public string FileType { get; set; } = string.Empty; // "jar", "pom", etc.
    public string FilePath { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    
    // POM-specific metadata
    public string? ParentGroupId { get; set; }
    public string? ParentArtifactId { get; set; }
    public string? ParentVersion { get; set; }
    public string? Description { get; set; }
    public string? ProjectUrl { get; set; }
    public string? License { get; set; }
    public string? Developers { get; set; }
} 