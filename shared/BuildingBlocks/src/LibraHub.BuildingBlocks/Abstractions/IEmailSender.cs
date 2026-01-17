namespace LibraHub.BuildingBlocks.Abstractions;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);

    Task SendEmailAsync(IEnumerable<string> emails, string subject, string body, CancellationToken cancellationToken = default);

    Task SendEmailWithTemplateAsync(
        string email,
        string subject,
        string templateName,
        object model,
        CancellationToken cancellationToken = default);

    Task SendEmailWithTemplateAsync(
        IEnumerable<string> emails,
        string subject,
        string templateName,
        object model,
        CancellationToken cancellationToken = default);
}
