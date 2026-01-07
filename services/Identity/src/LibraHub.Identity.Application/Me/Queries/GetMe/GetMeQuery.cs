using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Me.Queries.GetMe;

public record GetMeQuery : IRequest<Result<GetMeResponseDto>>;
