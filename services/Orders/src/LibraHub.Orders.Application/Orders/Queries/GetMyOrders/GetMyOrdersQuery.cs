using MediatR;

namespace LibraHub.Orders.Application.Orders.Queries.GetMyOrders;

public class GetMyOrdersQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<GetMyOrdersResponseDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
