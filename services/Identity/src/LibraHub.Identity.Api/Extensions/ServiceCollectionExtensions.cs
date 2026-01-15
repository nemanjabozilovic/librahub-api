using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Email;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.InternalAccess;
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
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                }));

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
        services.AddScoped<BuildingBlocks.Abstractions.IUnitOfWork, Infrastructure.Persistence.UnitOfWork>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();

        services.AddScoped<DatabaseSeeder>();
        services.AddRedisCache(configuration);
        services.AddLibraHubEmail(configuration);
        services.AddLibraHubMinioStorage(configuration);

        return services;
    }

    public static IServiceCollection AddIdentityJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt")).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SecurityOptions>().Bind(configuration.GetSection("Security")).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<TokenOptions>().Bind(configuration.GetSection(TokenOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<FrontendOptions>().Bind(configuration.GetSection(FrontendOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<IdentityOptions>().Bind(configuration.GetSection(IdentityOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddLibraHubJwtAuthentication(configuration);
        services.AddLibraHubInternalAccess(configuration);

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
