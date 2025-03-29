using System.Text.RegularExpressions;
using MavenRepositoryServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace MavenRepositoryServer.Controllers;

[ApiController]
[Route("maven2")]
public class CatchAllController: Controller
{
    private readonly RepositoryService _repositoryService;

    public CatchAllController(RepositoryService repositoryService)
    {
        _repositoryService = repositoryService;
    }

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD")]
    [Route("{*catchAll}")] // catch-all сегмент: захватывает всё, что идёт после /maven2/
    public async Task<IActionResult> HandleAllMethods(string catchAll)
    {
        var path = Request.Path.Value?.Trim();
        path = path?.Trim('/');
        
        if (Request.Method == "GET")
        {
            var fileExtension = Path.GetExtension(path) ?? string.Empty;
            
            if (path?.ToLower().EndsWith(fileExtension) == true)
            {
                var version = PackageInfoParser(path, fileExtension, out var artifactId, out var groupId, out var fileName);
                var content = await _repositoryService.GetArtifactAsync(groupId, artifactId, version, fileExtension.Replace(".", ""));

                var contentType = "text/plain";
                switch (fileExtension)
                {
                    case ".jar":
                    {
                        contentType = "application/octet-stream";
                        break;
                    }
                    case ".xml":
                    case ".pom":
                    case ".md5":
                    case ".sha1":
                    {
                        contentType = "application/xml";
                        break;
                    }
                }
                
                
                return File(content, contentType, $"{artifactId}-{version}{fileExtension}");
            }
            
            if (path?.ToLower().Contains(fileExtension) == true && fileExtension?.ToLower().Contains("xml") == true)
            {
                var version = PackageInfoParser(path, ".pom", out var artifactId, out var groupId, out var fileName);
                var content = await _repositoryService.GetPomFileAsync(groupId, artifactId, version);
                return File(content, "application/xml", $"{artifactId}-{version}.pom");
            }
                        
            return Ok();
        }
        
        if (Request.Method == "PUT") // possible post
        {
            Console.WriteLine("Method: " + Request.Path.Value);
            //maven2/com/example/maven-repository-server/1.0-SNAPSHOT/maven-repository-server-1.0-20250327.185037-1.pom.sha1
            if (path?.ToLower().EndsWith(".pom.sha1") == true)
            {
                var fileExtensions = "pom.sha1";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".pom.md5") == true)
            {
                var fileExtensions = "pom.md5";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".xml") == true)
            {
                var fileExtensions = "xml";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".xml.sha1") == true)
            {
                var fileExtensions = "xml.sha1";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".xml.md5") == true)
            {
                var fileExtensions = "xml.md5";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".jar") == true)
            {
                var fileExtensions = "jar";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".jar.sha1") == true)
            {
                var fileExtensions = "jar.sha1";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".jar.md5") == true)
            {
                var fileExtensions = "jar.md5";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".pom.sha1") == true)
            {
                var fileExtensions = "pom.sha1";
                await CommonArtifactUpload(path, fileExtensions);
                return Ok();
            }
            
            if (path?.ToLower().EndsWith(".pom") == true)
            {
                var fileExtensions = "pom";
                await CommonArtifactUpload(path, fileExtensions);
                //await _repositoryService.DeployPomFileAsync(groupId, artifactId, version, file);
                return Ok();
            }
        }
        
        return Ok($"Метод: {Request.Method}; Маршрут: {catchAll ?? "(пусто)"}");
    }

    private async Task CommonArtifactUpload(string path, string fileExtensions)
    {
        var version = PackageInfoParser(path, fileExtensions, out var artifactId, out var groupId, out var fileName);

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
                
        IFormFile file = new FormFile(memoryStream, 0, memoryStream.Length, "streamFile", fileName);
                    
        await  _repositoryService.DeployArtifactAsync(groupId, artifactId, version, fileExtensions, file);
    }

    private static string PackageInfoParser(string path, string fileExtensions, out string artifactId, out string groupId,
        out string fileName)
    {
        var arr = path?.Split('/');
        var version = arr[^2];
        artifactId = arr[^3];
        groupId = string.Join('.', arr[1..^3]);
        fileName = arr[^1];

        if (fileExtensions?.Contains("xml") == true)
        {
            var pattern = "/(?:\\d+(?:\\.\\d+)*)(?:-SNAPSHOT|-RC|-RELEASE)?/";
            var regex = new Regex(pattern);
            var match = regex.Match(path);
            if (!match.Success)
            {
                version = "";
                artifactId = arr[^2];
                groupId = string.Join('.', arr[1..^2]);
            }
        }

        return version;
    }
}