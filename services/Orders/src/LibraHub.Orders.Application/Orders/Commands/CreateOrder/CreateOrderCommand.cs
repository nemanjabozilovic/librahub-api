using MediatR;

namespace LibraHub.Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<BuildingBlocks.Results.Result<Guid>>
{
    public List<Guid> BookIds { get; init; } = new();
}
