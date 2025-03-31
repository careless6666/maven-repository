using System.ComponentModel.DataAnnotations;

namespace MavenRepositoryServer.Models;

public class Artifact
{
    [Key]
    public long Id { get; set; }
    
    public string GroupId { get; set; } = string.Empty;
    public string ArtifactId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    
    public virtual ICollection<ArtifactFile> Files { get; set; } = new List<ArtifactFile>();
} 