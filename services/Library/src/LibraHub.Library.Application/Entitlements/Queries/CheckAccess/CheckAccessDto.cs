namespace LibraHub.Library.Application.Entitlements.Queries.CheckAccess;

public class CheckAccessDto
{
    public bool HasAccess { get; init; }
    public string Status { get; init; } = string.Empty;
}
