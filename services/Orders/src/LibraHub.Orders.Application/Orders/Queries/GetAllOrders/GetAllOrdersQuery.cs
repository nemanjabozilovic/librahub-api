using MediatR;

namespace LibraHub.Orders.Application.Orders.Queries.GetAllOrders;

public class GetAllOrdersQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<GetAllOrdersResponseDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Period { get; init; } // "24h", "7d", "30d", or null for all
}
