namespace LibraHub.Identity.Application.Me;

public static class AvatarUrlHelper
{
    public static string? BuildAvatarUrl(string? avatar, Guid userId, string gatewayBaseUrl)
    {
        var relative = NormalizeAvatarPath(avatar, userId);
        if (string.IsNullOrWhiteSpace(relative))
        {
            return null;
        }

        return $"{gatewayBaseUrl.TrimEnd('/')}{relative}";
    }

    private static string? NormalizeAvatarPath(string? avatar, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            return null;
        }

        var path = avatar.Trim();

        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                path = uri.AbsolutePath;
            }
        }

        var marker = "/avatar/";
        var idx = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        var after = path[(idx + marker.Length)..];
        var fileName = Path.GetFileName(after);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return $"/api/users/{userId}/avatar/{fileName}";
    }
}
