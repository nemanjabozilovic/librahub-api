namespace LibraHub.Identity.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName => $"{FirstName} {LastName}".Trim();
    public string? Phone { get; private set; }
    public string? Avatar { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public bool EmailVerified { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedOutUntil { get; private set; }

    private readonly List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private User()
    { } // For EF Core

    public User(Guid id, string email, string passwordHash, string firstName, string lastName, string? phone = null, DateTime? dateOfBirth = null)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        EmailVerified = false;
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        _roles.Add(new UserRole(Id, Role.User));
    }

    public void MarkEmailAsVerified()
    {
        EmailVerified = true;
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    public void RecordFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedOutUntil = DateTime.UtcNow.Add(lockoutDuration);
        }
    }

    public bool IsLockedOut(DateTime utcNow)
    {
        return LockedOutUntil.HasValue && LockedOutUntil.Value > utcNow;
    }

    public void Disable(string reason)
    {
        Status = UserStatus.Disabled;
    }

    public void Enable()
    {
        Status = UserStatus.Active;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    public void AddRole(Role role)
    {
        if (!_roles.Any(r => r.Role == role))
        {
            _roles.Add(new UserRole(Id, role));
        }
    }

    public void RemoveRole(Role role)
    {
        var roleToRemove = _roles.FirstOrDefault(r => r.Role == role);
        if (roleToRemove != null)
        {
            _roles.Remove(roleToRemove);
        }
    }

    public bool HasRole(Role role)
    {
        return _roles.Any(r => r.Role == role);
    }

    public bool IsAdmin()
    {
        return HasRole(Role.Admin);
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void UpdateProfile(string firstName, string lastName, string? phone, DateTime? dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        DateOfBirth = dateOfBirth;
    }

    public void UpdateAvatar(string avatarUrl)
    {
        Avatar = avatarUrl;
    }
}

public class UserRole
{
    public Guid UserId { get; private set; }
    public Role Role { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private UserRole()
    { } // For EF Core

    public UserRole(Guid userId, Role role)
    {
        UserId = userId;
        Role = role;
        AssignedAt = DateTime.UtcNow;
    }
}
