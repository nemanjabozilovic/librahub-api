using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Auth.Dtos;
using MediatR;

namespace LibraHub.Identity.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password, string? RecaptchaToken = null) : IRequest<Result<AuthTokensDto>>;
