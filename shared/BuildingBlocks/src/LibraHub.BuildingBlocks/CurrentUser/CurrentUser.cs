using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Auth;
using Microsoft.AspNetCore.Http;

namespace LibraHub.BuildingBlocks.CurrentUser;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => User?.GetUserId();

    public string? Email => User?.GetEmail();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public IEnumerable<string> Roles => User?.GetRoles() ?? Enumerable.Empty<string>();
}
