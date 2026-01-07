using MediatR;

namespace LibraHub.Orders.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommand : IRequest<LibraHub.BuildingBlocks.Results.Result>
{
    public Guid OrderId { get; init; }
    public string? Reason { get; init; }
}
