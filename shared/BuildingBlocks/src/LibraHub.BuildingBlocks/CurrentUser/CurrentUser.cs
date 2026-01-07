using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Auth;
using Microsoft.AspNetCore.Http;

namespace LibraHub.BuildingBlocks.CurrentUser;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.GetUserId();
        }
    }

    public string? Email
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.GetEmail();
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }

    public bool IsInRole(string role)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(role) ?? false;
    }

    public IEnumerable<string> Roles
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.GetRoles() ?? Enumerable.Empty<string>();
        }
    }
}
