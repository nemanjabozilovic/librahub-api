namespace LibraHub.BuildingBlocks.Abstractions;

public interface IEmailTemplateService
{
    /// <summary>
    /// Retrieves an email template by name from BuildingBlocks assembly.
    /// </summary>
    /// <param name="templateName">The template name as configured in EmailTemplates section</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The template content as a string</returns>
    Task<string> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default);
}

