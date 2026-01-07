using LibraHub.BuildingBlocks.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace LibraHub.BuildingBlocks.Email;

public class EmailTemplateService(
    IConfiguration configuration,
    ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    private static readonly Assembly ThisAssembly = typeof(EmailTemplateService).Assembly;

    public async Task<string> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var templateFileName = configuration.GetSection("EmailTemplates")[templateName];

        if (string.IsNullOrWhiteSpace(templateFileName))
        {
            logger.LogWarning("Template name {TemplateName} not found in configuration", templateName);
            throw new InvalidOperationException($"Template name '{templateName}' not found in configuration");
        }

        var resourceName = $"{ThisAssembly.GetName().Name}.EmailTemplates.{templateFileName}";

        await using var stream = ThisAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            logger.LogError("Template resource {ResourceName} not found in assembly {AssemblyName}",
                resourceName, ThisAssembly.GetName().Name);
            throw new FileNotFoundException($"Template resource '{resourceName}' not found in assembly '{ThisAssembly.GetName().Name}'");
        }

        using var reader = new StreamReader(stream);
        var templateContent = await reader.ReadToEndAsync(cancellationToken);

        return templateContent;
    }
}
