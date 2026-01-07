using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Content.Application.Access.Queries.ValidateReadToken;

public record ValidateReadTokenQuery(string Token) : IRequest<Result<AccessGrantInfo>>;

public record AccessGrantInfo
{
    public Guid BookId { get; init; }
    public string? Format { get; init; }
    public int? Version { get; init; }
    public string Scope { get; init; } = string.Empty;
    public string ObjectKey { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}
