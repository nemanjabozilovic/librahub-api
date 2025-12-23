using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Email;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.BuildingBlocks.Storage;
using LibraHub.Identity.Application;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Infrastructure.Options;
using LibraHub.Identity.Infrastructure.Persistence;
using LibraHub.Identity.Infrastructure.Repositories;
using LibraHub.Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Identity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("Connection string 'IdentityDb' not found.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddIdentityApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IRegistrationCompletionTokenRepository, RegistrationCompletionTokenRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailVerificationTokenService, EmailVerificationTokenService>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<IRegistrationCompletionTokenService, RegistrationCompletionTokenService>();

        services.AddScoped<BuildingBlocks.Abstractions.IOutboxWriter, OutboxEventPublisher<IdentityDbContext>>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();

        services.AddScoped<DatabaseSeeder>();

        // Configure email using BuildingBlocks
        services.AddLibraHubEmail(configuration);

        // Configure Storage (MinIO)
        services.AddLibraHubMinioStorage(configuration);

        return services;
    }

    public static IServiceCollection AddIdentityJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT options not configured");

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SecurityOptions>(configuration.GetSection("Security"));
        services.Configure<TokenOptions>(configuration.GetSection(TokenOptions.SectionName));

        services.AddLibraHubJwtAuthentication(configuration);

        return services;
    }

    public static IServiceCollection AddIdentityRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubRabbitMq<OutboxPublisherWorkerHelper<IdentityDbContext>>(configuration);
    }

    public static IServiceCollection AddIdentityHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "IdentityDb");
    }
}
