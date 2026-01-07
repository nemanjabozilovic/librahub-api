namespace LibraHub.Library.Api.Dtos.Entitlements;

public class GrantEntitlementRequestDto
{
    public Guid UserId { get; init; }
    public Guid BookId { get; init; }
}
