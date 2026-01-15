namespace LibraHub.BuildingBlocks.Urls;

public static class UrlPathExtractor
{
    public static string? GetPathAfterSegment(string url, string segment)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(segment))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (!string.Equals(parts[i], segment, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i >= parts.Length - 1)
            {
                return null;
            }

            return string.Join("/", parts.Skip(i + 1));
        }

        return null;
    }
}


