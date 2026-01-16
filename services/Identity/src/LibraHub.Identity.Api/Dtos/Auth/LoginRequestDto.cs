namespace LibraHub.Identity.Api.Dtos.Auth;

public record LoginRequestDto(string Email, string Password, string? RecaptchaToken = null);
