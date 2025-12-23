namespace LibraHub.Identity.Api.Dtos.Auth;

public record ResetPasswordRequestDto(string Token, string NewPassword, string ConfirmPassword);

