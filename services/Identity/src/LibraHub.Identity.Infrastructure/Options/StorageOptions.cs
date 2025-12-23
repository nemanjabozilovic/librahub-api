namespace LibraHub.Identity.Infrastructure.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;
    public string AvatarsBucketName { get; set; } = "avatars";
    public string ApiBaseUrl { get; set; } = string.Empty;
}

