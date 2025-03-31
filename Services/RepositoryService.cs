using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using MavenRepositoryServer.Data;
using MavenRepositoryServer.Models;

namespace MavenRepositoryServer.Services;

public class RepositoryService
{
    private readonly RepositoryContext _context;
    private readonly string _repositoryBase;

    public RepositoryService(RepositoryContext context, IConfiguration configuration)
    {
        _context = context;
        _repositoryBase = configuration["MavenRepository:BasePath"] ?? "./repository";
        Directory.CreateDirectory(_repositoryBase);
    }

    public async Task DeployArtifactAsync(string groupId, string artifactId, string version, string packaging, IFormFile file)
    {
        var artifact = await GetOrCreateArtifactAsync(groupId, artifactId, version, packaging);
        var filePath = CreateArtifactPath(groupId, artifactId, version, packaging);
        var targetPath = Path.Combine(_repositoryBase, filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        using (var stream = new FileStream(targetPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var checksum = await CalculateChecksumAsync(file);

        var artifactFile = new ArtifactFile
        {
            ArtifactId = artifact.Id,
            FileType = packaging,
            FilePath = filePath,
            Checksum = checksum,
            Version = version,
        };

        _context.ArtifactFiles.Add(artifactFile);
        await _context.SaveChangesAsync();
    }

    public async Task DeployPomFileAsync(string groupId, string artifactId, string version, IFormFile pomFile)
    {
        var artifact = await GetOrCreateArtifactAsync(groupId, artifactId, version, "pom");
        var pomFilePath = CreatePomFilePath(groupId, artifactId, version);
        var targetPath = Path.Combine(_repositoryBase, pomFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        using (var stream = new FileStream(targetPath, FileMode.Create))
        {
            await pomFile.CopyToAsync(stream);
        }

        var pomChecksum = await CalculateChecksumAsync(pomFile);
        var pomContent = await File.ReadAllTextAsync(targetPath);
        var pomMetadata = ParsePomMetadata(pomContent);

        var artifactFile = new ArtifactFile
        {
            ArtifactId = artifact.Id,
            FileType = "pom",
            FilePath = pomFilePath,
            Checksum = pomChecksum,
            ParentGroupId = pomMetadata.ParentGroupId,
            ParentArtifactId = pomMetadata.ParentArtifactId,
            ParentVersion = pomMetadata.ParentVersion,
            Description = pomMetadata.Description,
            ProjectUrl = pomMetadata.ProjectUrl,
            License = pomMetadata.License,
            Developers = pomMetadata.Developers
        };

        _context.ArtifactFiles.Add(artifactFile);
        await _context.SaveChangesAsync();
    }

    public async Task<byte[]> GetArtifactAsync(string groupId, string artifactId, string version, string packaging)
    {
        var artifact = await _context.Artifacts
            .Include(a => a.Files)
            .FirstOrDefaultAsync(a => a.GroupId == groupId && 
                                    a.ArtifactId == artifactId && 
                                    a.Version == version);

        if (artifact == null)
        {
            return null;
        }

        var artifactFile = artifact.Files.FirstOrDefault(f => f.FileType == packaging);
        if (artifactFile == null)
        {
            throw new FileNotFoundException($"File of type {packaging} not found");
        }

        var filePath = Path.Combine(_repositoryBase, artifactFile.FilePath);
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task<byte[]> GetPomFileAsync(string groupId, string artifactId, string version)
    {
        var artifact = await _context.Artifacts
            .Include(a => a.Files)
            .FirstOrDefaultAsync(a => a.GroupId == groupId && 
                                    a.ArtifactId == artifactId && 
                                    a.Version == version);

        if (artifact == null)
        {
            throw new FileNotFoundException("Artifact not found");
        }

        var pomFile = artifact.Files.FirstOrDefault(f => f.FileType == "pom");
        if (pomFile == null)
        {
            throw new FileNotFoundException("POM file not found");
        }

        var filePath = Path.Combine(_repositoryBase, pomFile.FilePath);
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task<IEnumerable<Artifact>> SearchArtifactsAsync(string? groupId, string? artifactId, string? version)
    {
        var query = _context.Artifacts
            .Include(a => a.Files)
            .AsQueryable();

        if (!string.IsNullOrEmpty(groupId))
        {
            query = query.Where(a => a.GroupId.Contains(groupId));
        }

        if (!string.IsNullOrEmpty(artifactId))
        {
            query = query.Where(a => a.ArtifactId.Contains(artifactId));
        }

        if (!string.IsNullOrEmpty(version))
        {
            query = query.Where(a => a.Version.Contains(version));
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Artifact>> SearchByGroupIdAsync(string groupId)
    {
        return await _context.Artifacts
            .Include(a => a.Files)
            .Where(a => a.GroupId.Contains(groupId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Artifact>> SearchByArtifactIdAsync(string artifactId)
    {
        return await _context.Artifacts
            .Include(a => a.Files)
            .Where(a => a.ArtifactId.Contains(artifactId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Artifact>> SearchByVersionAsync(string version)
    {
        return await _context.Artifacts
            .Include(a => a.Files)
            .Where(a => a.Version.Contains(version))
            .ToListAsync();
    }

    private async Task<Artifact> GetOrCreateArtifactAsync(string groupId, string artifactId, string version, string packaging)
    {
        var artifact = await _context.Artifacts
            .FirstOrDefaultAsync(a => a.GroupId == groupId && 
                                    a.ArtifactId == artifactId && 
                                    a.Version == version);

        if (artifact == null)
        {
            artifact = new Artifact
            {
                GroupId = groupId,
                ArtifactId = artifactId,
                Version = version,
                Packaging = packaging
            };
            _context.Artifacts.Add(artifact);
            await _context.SaveChangesAsync();
        }

        return artifact;
    }

    private string CreateArtifactPath(string groupId, string artifactId, string version, string packaging)
    {
        if (packaging?.ToLower().StartsWith("xml") == true)
        {
            return Path.Combine(
                groupId.Replace('.', Path.DirectorySeparatorChar),
                artifactId,
                version,
                $"maven-metadata.{packaging}"
            );
        }
        
        return Path.Combine(
            groupId.Replace('.', Path.DirectorySeparatorChar),
            artifactId,
            version,
            $"{artifactId}-{version}.{packaging}"
        );
    }

    private string CreatePomFilePath(string groupId, string artifactId, string version)
    {
        return Path.Combine(
            groupId.Replace('.', Path.DirectorySeparatorChar),
            artifactId,
            version,
            $"{artifactId}-{version}.pom"
        );
    }

    private async Task<string> CalculateChecksumAsync(IFormFile file)
    {
        using var sha1 = SHA1.Create();
        using var stream = file.OpenReadStream();
        var hash = await sha1.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }

    private PomMetadata ParsePomMetadata(string pomContent)
    {
        var doc = XDocument.Parse(pomContent);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var parent = doc.Root?.Element(ns + "parent");
        var project = doc.Root;

        return new PomMetadata
        {
            ParentGroupId = parent?.Element(ns + "groupId")?.Value,
            ParentArtifactId = parent?.Element(ns + "artifactId")?.Value,
            ParentVersion = parent?.Element(ns + "version")?.Value,
            Description = project?.Element(ns + "description")?.Value,
            ProjectUrl = project?.Element(ns + "url")?.Value,
            License = project?.Element(ns + "licenses")?.Element(ns + "license")?.Element(ns + "name")?.Value,
            Developers = string.Join(", ", project?.Element(ns + "developers")?.Elements(ns + "developer")
                .Select(d => d.Element(ns + "name")?.Value)
                .Where(n => n != null) ?? Array.Empty<string>())
        };
    }

    private class PomMetadata
    {
        public string? ParentGroupId { get; set; }
        public string? ParentArtifactId { get; set; }
        public string? ParentVersion { get; set; }
        public string? Description { get; set; }
        public string? ProjectUrl { get; set; }
        public string? License { get; set; }
        public string? Developers { get; set; }
    }
} 