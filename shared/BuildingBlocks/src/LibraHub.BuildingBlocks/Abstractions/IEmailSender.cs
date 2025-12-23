namespace LibraHub.BuildingBlocks.Abstractions;

public interface IEmailSender
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients.
    /// </summary>
    Task SendEmailAsync(IEnumerable<string> emails, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template to a single recipient.
    /// </summary>
    Task SendEmailWithTemplateAsync(
        string email,
        string subject,
        string templateName,
        object model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template to multiple recipients.
    /// </summary>
    Task SendEmailWithTemplateAsync(
        IEnumerable<string> emails,
        string subject,
        string templateName,
        object model,
        CancellationToken cancellationToken = default);
}

