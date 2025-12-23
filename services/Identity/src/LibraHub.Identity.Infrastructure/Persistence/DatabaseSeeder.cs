using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraHub.Identity.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDefaultAdminAsync(cancellationToken);
    }

    private async Task SeedDefaultAdminAsync(CancellationToken cancellationToken)
    {
        const string defaultAdminEmail = "admin@librahub.com";
        const string defaultAdminPassword = "Admin123!";

        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == defaultAdminEmail, cancellationToken);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Default admin user already exists");
            return;
        }

        var passwordHash = _passwordHasher.HashPassword(defaultAdminPassword);
        var adminUser = new User(
            id: Guid.NewGuid(),
            email: defaultAdminEmail,
            passwordHash: passwordHash,
            firstName: "Admin",
            lastName: "User");

        adminUser.MarkEmailAsVerified();
        adminUser.RemoveRole(Role.User); // Remove default User role
        adminUser.AddRole(Role.Admin);

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Default admin user created: {Email}", defaultAdminEmail);
    }
}
