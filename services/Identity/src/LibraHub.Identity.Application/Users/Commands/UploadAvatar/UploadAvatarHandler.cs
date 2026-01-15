using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.BuildingBlocks.Urls;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Commands.UploadAvatar;

public class UploadAvatarHandler(
    IUserRepository userRepository,
    IObjectStorage objectStorage,
    IConfiguration configuration,
    ILogger<UploadAvatarHandler> logger) : IRequestHandler<UploadAvatarCommand, Result<string>>
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public async Task<Result<string>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var fileValidation = ValidateFile(request.File);
        if (fileValidation.IsFailure)
        {
            return Result.Failure<string>(fileValidation.Error!);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<string>(Error.NotFound("User not found"));
        }

        var bucketName = GetRequiredConfigValue(configuration, "Storage:AvatarsBucketName");
        if (bucketName.IsFailure) return Result.Failure<string>(bucketName.Error!);

        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            try
            {
                var oldPath = UrlPathExtractor.GetPathAfterSegment(user.Avatar, "avatar");
                if (!string.IsNullOrWhiteSpace(oldPath))
                {
                    var normalizedOldKey = oldPath.Contains('/', StringComparison.Ordinal)
                        ? oldPath
                        : $"users/{request.UserId}/avatar/{oldPath}";

                    await objectStorage.DeleteAsync(bucketName.Value!, normalizedOldKey, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old avatar for user {UserId}", request.UserId);
            }
        }

        var fileExtension = Path.GetExtension(request.File!.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var objectKey = $"users/{request.UserId}/avatar/{fileName}";

        try
        {
            await objectStorage.UploadAsync(
                bucketName.Value!,
                objectKey,
                request.File.OpenReadStream(),
                request.File.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload avatar for user {UserId}", request.UserId);
            return Result.Failure<string>(Error.Validation("Failed to upload avatar"));
        }

        var avatarUrl = $"/api/users/{request.UserId}/avatar/{fileName}";
        user.UpdateAvatar(avatarUrl);

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(avatarUrl);
    }

    private static Result ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return Result.Failure(Error.Validation("File is required"));
        }

        if (file.Length > MaxFileSize)
        {
            return Result.Failure(Error.Validation($"File size must not exceed {MaxFileSize / 1024 / 1024}MB"));
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(fileExtension))
        {
            return Result.Failure(Error.Validation($"Allowed file extensions: {string.Join(", ", AllowedExtensions)}"));
        }

        return Result.Success();
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
}
