namespace LibraHub.Orders.Api.Dtos.Payments;

public class CapturePaymentRequestDto
{
    public Guid PaymentId { get; init; }
    public string ProviderReference { get; init; } = string.Empty;
}
