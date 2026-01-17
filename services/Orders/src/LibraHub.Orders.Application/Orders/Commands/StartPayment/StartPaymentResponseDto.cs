namespace LibraHub.Orders.Application.Orders.Commands.StartPayment;

public record StartPaymentResponseDto
{
    public Guid PaymentId { get; init; }
    public string ProviderReference { get; init; } = string.Empty;
}
