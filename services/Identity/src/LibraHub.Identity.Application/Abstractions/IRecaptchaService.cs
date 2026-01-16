namespace LibraHub.Identity.Application.Abstractions;

public interface IRecaptchaService
{
    Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default);
}
