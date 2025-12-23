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
        var emailList = emails.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

        if (emailList.Count == 0)
        {
            logger.LogWarning("No valid email addresses provided, skipping email send");
            return;
        }

        try
        {
            var emailBuilder = fluentEmail
                .Subject(subject)
                .Body(body, true); // true = HTML body

            // Add all recipients
            foreach (var email in emailList)
            {
                emailBuilder.To(email);
            }

            var response = await emailBuilder.SendAsync(cancellationToken);

            if (!response.Successful)
            {
                logger.LogError("Failed to send email to {Emails}. Errors: {Errors}",
                    string.Join(", ", emailList), string.Join(", ", response.ErrorMessages));
            }
            else
            {
                logger.LogInformation("Email sent successfully to {Emails}", string.Join(", ", emailList));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {Emails}", string.Join(", ", emailList));
        }
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
        var emailList = emails.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

        if (emailList.Count == 0)
        {
            logger.LogWarning("No valid email addresses provided, skipping email send");
            return;
        }

        try
        {
            var templateContent = await templateService.GetTemplateByNameAsync(templateName, cancellationToken);

            var emailBuilder = fluentEmail
                .Subject(subject)
                .UsingTemplate(templateContent, model);

            // Add all recipients
            foreach (var email in emailList)
            {
                emailBuilder.To(email);
            }

            var response = await emailBuilder.SendAsync(cancellationToken);

            if (!response.Successful)
            {
                logger.LogError("Failed to send email to {Emails} using template {TemplateName}. Errors: {Errors}",
                    string.Join(", ", emailList), templateName, string.Join(", ", response.ErrorMessages));
            }
            else
            {
                logger.LogInformation("Email sent successfully to {Emails} using template {TemplateName}",
                    string.Join(", ", emailList), templateName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {Emails} with template {TemplateName}",
                string.Join(", ", emailList), templateName);
            // Don't throw - emails are best-effort
        }
    }
}

