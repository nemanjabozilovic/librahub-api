using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Content.Application.Access.Commands.CreateReadToken;

public record CreateReadTokenCommand(
    Guid BookId,
    string? Format = null,
    int? Version = null) : IRequest<Result<string>>;
