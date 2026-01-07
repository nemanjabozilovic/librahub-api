using MediatR;

namespace LibraHub.Orders.Application.Orders.Queries.GetOrder;

public class GetOrderQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<OrderDto>>
{
    public Guid OrderId { get; init; }
}
