using MediatR;

namespace LibraHub.Orders.Application.Orders.Commands.RefundOrder;

public class RefundOrderCommand : IRequest<LibraHub.BuildingBlocks.Results.Result>
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
