namespace LibraHub.BuildingBlocks.Abstractions;

public interface IEmailTemplateService
{
    Task<string> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default);
}
