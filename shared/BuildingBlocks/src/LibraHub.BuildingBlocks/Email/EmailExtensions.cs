using LibraHub.BuildingBlocks.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Mail;

namespace LibraHub.BuildingBlocks.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddLibraHubEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EmailConfigOptions>()
            .Bind(configuration.GetSection(EmailConfigOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var emailConfig = configuration.GetSection(EmailConfigOptions.SectionName).Get<EmailConfigOptions>() ?? throw new InvalidOperationException("Email configuration is missing.");
        var smtpClient = new SmtpClient(emailConfig.Host)
        {
            Port = emailConfig.Port,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = emailConfig.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(emailConfig.Username) && !string.IsNullOrWhiteSpace(emailConfig.Password))
        {
            smtpClient.Credentials = new NetworkCredential(emailConfig.Username, emailConfig.Password);
            smtpClient.UseDefaultCredentials = false;
        }
        else
        {
            smtpClient.UseDefaultCredentials = true;
        }

        services
            .AddFluentEmail(emailConfig.From)
            .AddRazorRenderer()
            .AddSmtpSender(smtpClient);

        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}
