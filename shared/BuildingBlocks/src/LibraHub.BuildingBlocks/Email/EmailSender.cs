using FluentEmail.Core;
using LibraHub.BuildingBlocks.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.BuildingBlocks.Email;

public class EmailSender(
    IFluentEmail fluentEmail,
    IEmailTemplateService templateService,
    ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(new[] { email }, subject, body, cancellationToken);
    }

    public async Task SendEmailAsync(IEnumerable<string> emails, string subject, string body, CancellationToken cancellationToken = default)
    {
        var emailList = ValidateEmails(emails);
        if (emailList == null)
        {
            return;
        }

        var emailBuilder = fluentEmail
            .Subject(subject)
            .Body(body, true);

        await SendEmailInternalAsync(emailBuilder, emailList, cancellationToken);
    }

    public async Task SendEmailWithTemplateAsync(
        string email,
        string subject,
        string templateName,
        object model,
        CancellationToken cancellationToken = default)
    {
        await SendEmailWithTemplateAsync(new[] { email }, subject, templateName, model, cancellationToken);
    }

    public async Task SendEmailWithTemplateAsync(
        IEnumerable<string> emails,
        string subject,
        string templateName,
        object model,
        CancellationToken cancellationToken = default)
    {
        var emailList = ValidateEmails(emails);
        if (emailList == null)
        {
            return;
        }

        try
        {
            var templateContent = await templateService.GetTemplateByNameAsync(templateName, cancellationToken);

            var emailBuilder = fluentEmail
                .Subject(subject)
                .UsingTemplate(templateContent, model);

            await SendEmailInternalAsync(emailBuilder, emailList, cancellationToken, templateName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {Emails} with template {TemplateName}",
                string.Join(", ", emailList), templateName);
        }
    }

    private List<string>? ValidateEmails(IEnumerable<string> emails)
    {
        var emailList = emails.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

        if (emailList.Count == 0)
        {
            logger.LogWarning("No valid email addresses provided, skipping email send");
            return null;
        }

        return emailList;
    }

    private async Task SendEmailInternalAsync(
        IFluentEmail emailBuilder,
        List<string> emailList,
        CancellationToken cancellationToken,
        string? templateName = null)
    {
        foreach (var email in emailList)
        {
            emailBuilder.To(email);
        }

        var emailsString = string.Join(", ", emailList);

        try
        {
            var response = await emailBuilder.SendAsync(cancellationToken);

            if (!response.Successful)
            {
                var errorMessage = templateName != null
                    ? $"Failed to send email to {{Emails}} using template {templateName}. Errors: {{Errors}}"
                    : "Failed to send email to {Emails}. Errors: {Errors}";

                logger.LogError(errorMessage, emailsString, string.Join(", ", response.ErrorMessages));
            }
            else
            {
                var successMessage = templateName != null
                    ? $"Email sent successfully to {{Emails}} using template {templateName}"
                    : "Email sent successfully to {Emails}";

                logger.LogInformation(successMessage, emailsString);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = templateName != null
                ? $"Error sending email to {{Emails}} with template {templateName}"
                : "Error sending email to {Emails}";

            logger.LogError(ex, errorMessage, emailsString);
        }
    }
}
