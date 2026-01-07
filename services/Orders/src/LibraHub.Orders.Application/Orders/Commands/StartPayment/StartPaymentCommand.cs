using MediatR;

namespace LibraHub.Orders.Application.Orders.Commands.StartPayment;

public class StartPaymentCommand : IRequest<LibraHub.BuildingBlocks.Results.Result<Guid>>
{
    public Guid OrderId { get; init; }
    public string Provider { get; init; } = string.Empty;
}
