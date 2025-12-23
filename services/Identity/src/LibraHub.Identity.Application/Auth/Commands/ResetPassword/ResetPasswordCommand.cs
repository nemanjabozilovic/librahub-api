using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword, string ConfirmPassword) : IRequest<Result>;

