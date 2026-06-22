using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Identity.Application.Abstractions;

public static class RecaptchaServiceExtensions
{
    public static async Task<Result> ValidateAsync(
        this IRecaptchaService recaptchaService,
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure(Error.Validation("reCAPTCHA token is required"));
        }

        var isValid = await recaptchaService.VerifyAsync(token, cancellationToken);
        if (!isValid)
        {
            return Result.Failure(Error.Validation("reCAPTCHA verification failed"));
        }

        return Result.Success();
    }
}
