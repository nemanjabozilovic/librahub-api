using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Auth.Commands.ResendVerificationEmail;

public record ResendVerificationEmailCommand(string Email) : IRequest<Result>;
