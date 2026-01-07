using MediatR;

namespace LibraHub.Orders.Application.Orders.Commands.CapturePayment;

public class CapturePaymentCommand : IRequest<LibraHub.BuildingBlocks.Results.Result>
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string ProviderReference { get; init; } = string.Empty;
}
