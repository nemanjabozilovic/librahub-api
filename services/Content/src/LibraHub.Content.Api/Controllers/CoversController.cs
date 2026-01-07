using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Content.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Api.Controllers;

[ApiController]
[Route("api/covers")]
public class CoversController(
    IObjectStorage objectStorage,
    IOptions<UploadOptions> uploadOptions) : ControllerBase
{
    [HttpGet("{*coverRef}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetCover(
        string coverRef,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(coverRef))
        {
            return NotFound();
        }

        try
        {
            var stream = await objectStorage.DownloadAsync(
                uploadOptions.Value.CoversBucketName,
                coverRef,
                cancellationToken);

            var contentType = GetContentType(coverRef);
            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch
        {
            return NotFound();
        }
    }

    private static string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
