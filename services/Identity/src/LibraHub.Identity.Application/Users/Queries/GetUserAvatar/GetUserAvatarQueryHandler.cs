using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUserAvatar;

public class GetUserAvatarQueryHandler(
    IObjectStorage objectStorage,
    IConfiguration configuration) : IRequestHandler<GetUserAvatarQuery, Result<UserAvatarFileDto>>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public async Task<Result<UserAvatarFileDto>> Handle(GetUserAvatarQuery request, CancellationToken cancellationToken)
    {
        var fileName = request.FileName?.Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure<UserAvatarFileDto>(Error.Validation("File name is required"));
        }

        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains("..", StringComparison.Ordinal))
        {
            return Result.Failure<UserAvatarFileDto>(Error.Validation("Invalid file name"));
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Failure<UserAvatarFileDto>(Error.Validation("Invalid file extension"));
        }

        var bucketName = GetRequiredConfigValue(configuration, "Storage:AvatarsBucketName");
        if (bucketName.IsFailure)
        {
            return Result.Failure<UserAvatarFileDto>(bucketName.Error!);
        }

        var objectKey = $"users/{request.UserId}/avatar/{fileName}";

        if (!await objectStorage.ExistsAsync(bucketName.Value!, objectKey, cancellationToken))
        {
            return Result.Failure<UserAvatarFileDto>(Error.NotFound("Avatar not found"));
        }

        var stream = await objectStorage.DownloadAsync(bucketName.Value!, objectKey, cancellationToken);
        return Result.Success(new UserAvatarFileDto(stream, GetContentType(fileName)));
    }

    private static Result<string> GetRequiredConfigValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<string>(Error.Validation($"{key} configuration is required"));
        }

        return Result.Success(value);
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
