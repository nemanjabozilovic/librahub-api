using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraHub.Identity.Infrastructure.Persistence;

public class DatabaseSeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDefaultAdminAsync(cancellationToken);
    }

    private async Task SeedDefaultAdminAsync(CancellationToken cancellationToken)
    {
        const string defaultAdminEmail = "admin@librahub.com";
        const string defaultAdminPassword = "RHV2YWogZ2E=";

        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Email == defaultAdminEmail, cancellationToken);

        if (existingAdmin != null)
        {
            logger.LogInformation("Default admin user already exists");
            return;
        }

        var passwordHash = passwordHasher.HashPassword(defaultAdminPassword);
        var adminUser = new User(
            id: Guid.NewGuid(),
            email: defaultAdminEmail,
            passwordHash: passwordHash,
            firstName: "Admin",
            lastName: "User");

        adminUser.MarkEmailAsVerified();
        adminUser.AddRole(Role.Admin);

        context.Users.Add(adminUser);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Default admin user created: {Email}", defaultAdminEmail);
    }
}
