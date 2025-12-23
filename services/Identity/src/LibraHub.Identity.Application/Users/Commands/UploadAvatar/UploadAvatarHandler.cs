using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
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
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public async Task<Result<string>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        // Validate file
        if (request.File == null || request.File.Length == 0)
        {
            return Result.Failure<string>(Error.Validation("File is required"));
        }

        if (request.File.Length > MaxFileSize)
        {
            return Result.Failure<string>(Error.Validation($"File size must not exceed {MaxFileSize / 1024 / 1024}MB"));
        }

        var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(fileExtension))
        {
            return Result.Failure<string>(Error.Validation($"Allowed file extensions: {string.Join(", ", AllowedExtensions)}"));
        }

        // Check if user exists
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<string>(Error.NotFound("User not found"));
        }

        var bucketName = configuration["Storage:AvatarsBucketName"];
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return Result.Failure<string>(Error.Validation("Storage:AvatarsBucketName configuration is required"));
        }

        var apiBaseUrl = configuration["Storage:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            return Result.Failure<string>(Error.Validation("Storage:ApiBaseUrl configuration is required"));
        }

        // Delete old avatar if exists
        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            try
            {
                var oldObjectKey = ExtractObjectKeyFromUrl(user.Avatar);
                if (!string.IsNullOrWhiteSpace(oldObjectKey))
                {
                    await objectStorage.DeleteAsync(bucketName, oldObjectKey, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old avatar for user {UserId}", request.UserId);
            }
        }

        // Generate object key
        var objectKey = $"users/{request.UserId}/avatar/{Guid.NewGuid()}{fileExtension}";

        // Upload to object storage
        try
        {
            await objectStorage.UploadAsync(
                bucketName,
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

        // Generate avatar URL using API base URL
        var avatarUrl = $"{apiBaseUrl.TrimEnd('/')}/api/users/{request.UserId}/avatar/{objectKey}";
        user.UpdateAvatar(avatarUrl);

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(avatarUrl);
    }

    private static string? ExtractObjectKeyFromUrl(string url)
    {
        // Extract object key from URL
        // Format: {apiBaseUrl}/api/users/{userId}/avatar/{objectKey}
        var uri = new Uri(url);
        var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Find "avatar" segment and extract everything after it
        var avatarIndex = Array.IndexOf(pathParts, "avatar");
        if (avatarIndex >= 0 && avatarIndex < pathParts.Length - 1)
        {
            return string.Join("/", pathParts.Skip(avatarIndex + 1));
        }

        return null;
    }
}

