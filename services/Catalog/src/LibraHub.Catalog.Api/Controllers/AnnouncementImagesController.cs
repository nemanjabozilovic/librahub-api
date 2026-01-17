using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LibraHub.Catalog.Api.Controllers;

[ApiController]
[Route("announcement-images")]
public class AnnouncementImagesController(
    IObjectStorage objectStorage,
    IOptions<UploadOptions> uploadOptions) : ControllerBase
{
    [HttpGet("{*imageRef}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetImage(
        string imageRef,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageRef))
        {
            return NotFound();
        }

        try
        {
            var stream = await objectStorage.DownloadAsync(
                uploadOptions.Value.AnnouncementImagesBucketName,
                imageRef,
                cancellationToken);

            var contentType = GetContentType(imageRef);
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
