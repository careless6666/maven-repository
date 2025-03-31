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
    [Route("{*catchAll}")] 
    public async Task<IActionResult> HandleAllMethods(string catchAll)
    {
        var path = Request.Path.Value?.Trim();
        path = path?.Trim('/');
        
        Console.WriteLine("path: " + path);
        
        if (Request.Method == "GET")
        {
            var fileExtension = GetFileExtensionForPath(path);
            
            if (path?.ToLower().EndsWith(fileExtension) == true)
            {
                var version = PackageInfoParser(path, fileExtension, out var artifactId, out var groupId, out var fileName);
                var content = await _repositoryService.GetArtifactAsync(groupId, artifactId, version, fileExtension);

                if(content == null)
                    return NotFound();
                
                var contentType = "text/plain";
                switch (fileExtension)
                {
                    case "jar":
                    {
                        contentType = "application/octet-stream";
                        break;
                    }
                    case "xml":
                    case "pom":
                    case "md5":
                    case "sha1":
                    {
                        contentType = "application/xml";
                        break;
                    }
                }
                
                return File(content, contentType, $"{artifactId}-{version}{fileExtension}");
            }
                        
            return Ok();
        }
        
        if (Request.Method == "PUT") // possible post
        {
            Console.WriteLine("Method: " + Request.Path.Value);
            // http://localhost:5000/maven2/com/example/maven-repository-server/1.0-SNAPSHOT/maven-repository-server-1.0-20250327.185037-1.pom.sha1
            // http://localhost:5000/maven2/com/example/maven-repository-server/1.0-SNAPSHOT/maven-repository-server-1.0-20250331.192832-3.jar
            // 

            var fileExtension = GetFileExtensionForPath(path);

            if (fileExtension != null)
            {
                await CommonArtifactUpload(path, fileExtension);
                return Ok();
            }
            else
            {
                Console.WriteLine("Bad request for path " + path);
                return BadRequest("Unsupported file type.");
            }
        }
        
        return Ok($"Метод: {Request.Method}; Маршрут: {catchAll ?? "(empty)"}");
    }

    private static string? GetFileExtensionForPath(string? path)
    {
        string fileExtension = path?.ToLower() switch
        {
            var p when p.EndsWith(".pom.sha1") => "pom.sha1",
            var p when p.EndsWith(".xml.sha1") => "xml.sha1",
            var p when p.EndsWith(".jar.sha1") => "jar.sha1",
            var p when p.EndsWith(".pom.md5") => "pom.md5",
            var p when p.EndsWith(".xml.md5") => "xml.md5",
            var p when p.EndsWith(".jar.md5") => "jar.md5",
            var p when p.EndsWith(".xml") => "xml",
            var p when p.EndsWith(".jar") => "jar",
            var p when p.EndsWith(".pom") => "pom",
            _ => String.Empty // or handle an unsupported extension scenario
        };
        return fileExtension;
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