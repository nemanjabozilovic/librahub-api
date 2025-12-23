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
        var emailConfig = configuration.GetSection("EmailConfig");
        var host = emailConfig["Host"] ?? "127.0.0.1";
        var port = int.Parse(emailConfig["Port"] ?? "25");
        var from = emailConfig["From"] ?? "noreply@librahub.local";
        var username = emailConfig["Username"];
        var password = emailConfig["Password"];
        var enableSsl = bool.TryParse(emailConfig["EnableSsl"], out var ssl) && ssl;

        var smtpClient = new SmtpClient(host)
        {
            Port = port,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            smtpClient.Credentials = new NetworkCredential(username, password);
            smtpClient.UseDefaultCredentials = false;
        }
        else
        {
            smtpClient.UseDefaultCredentials = true;
        }

        services
            .AddFluentEmail(from)
            .AddRazorRenderer()
            .AddSmtpSender(smtpClient);

        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}

