using Microsoft.AspNetCore.Mvc;
using MavenRepositoryServer.Models;
using MavenRepositoryServer.Services;

namespace MavenRepositoryServer.Controllers;

[ApiController]
[Route("maven23")]
public class RepositoryController : ControllerBase
{
    private readonly RepositoryService _repositoryService;

    public RepositoryController(RepositoryService repositoryService)
    {
        _repositoryService = repositoryService;
    }

    [HttpPut("{groupId}/{artifactId}/{version}/{artifactIdName}-{versionName}.{packaging}")]
    public async Task<IActionResult> DeployArtifact(
        string groupId,
        string artifactId,
        string version,
        string packaging,
        IFormFile file)
    {
        if (packaging?.ToLower().Trim() == "pom")
        {
            await _repositoryService.DeployPomFileAsync(groupId, artifactId, version, file);
            return Ok();
        }
        
        await _repositoryService.DeployArtifactAsync(groupId, artifactId, version, packaging, file);
        return Ok();
    }

    [HttpGet("{groupId}/{artifactId}/{version}/{artifactIdName}-{versionName}.{packaging}")]
    public async Task<IActionResult> GetArtifact(
        string groupId,
        string artifactId,
        string version,
        string packaging)
    {
        var content = await _repositoryService.GetArtifactAsync(groupId, artifactId, version, packaging);
        return File(content, "application/octet-stream", $"{artifactId}-{version}.{packaging}");
    }

    [HttpGet("{groupId}/{artifactId}/{version}/{artifactIdName}-{versionName}.pom")]
    public async Task<IActionResult> GetPomFile(
        string groupId,
        string artifactId,
        string version)
    {
        var content = await _repositoryService.GetPomFileAsync(groupId, artifactId, version);
        return File(content, "application/xml", $"{artifactId}-{version}.pom");
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Artifact>>> SearchArtifacts(
        [FromQuery] string? groupId,
        [FromQuery] string? artifactId,
        [FromQuery] string? version)
    {
        var artifacts = await _repositoryService.SearchArtifactsAsync(groupId, artifactId, version);
        return Ok(artifacts);
    }

    [HttpGet("search/groupId/{groupId}")]
    public async Task<ActionResult<IEnumerable<Artifact>>> SearchByGroupId(string groupId)
    {
        var artifacts = await _repositoryService.SearchByGroupIdAsync(groupId);
        return Ok(artifacts);
    }

    [HttpGet("search/artifactId/{artifactId}")]
    public async Task<ActionResult<IEnumerable<Artifact>>> SearchByArtifactId(string artifactId)
    {
        var artifacts = await _repositoryService.SearchByArtifactIdAsync(artifactId);
        return Ok(artifacts);
    }

    [HttpGet("search/version/{version}")]
    public async Task<ActionResult<IEnumerable<Artifact>>> SearchByVersion(string version)
    {
        var artifacts = await _repositoryService.SearchByVersionAsync(version);
        return Ok(artifacts);
    }
} 