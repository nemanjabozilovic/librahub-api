using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;

namespace LibraHub.Orders.Application.Abstractions;

public interface IPaymentGateway
{
    Task<PaymentResult> InitiatePaymentAsync(
        Guid orderId,
        Money amount,
        PaymentProvider provider,
        CancellationToken cancellationToken = default);

    Task<PaymentResult> CapturePaymentAsync(
        string providerReference,
        CancellationToken cancellationToken = default);
}

public class PaymentResult
{
    public bool Success { get; init; }
    public string? ProviderReference { get; init; }
    public string? FailureReason { get; init; }

    public static PaymentResult Succeeded(string providerReference) => new()
    {
        Success = true,
        ProviderReference = providerReference
    };

    public static PaymentResult Failed(string reason) => new()
    {
        Success = false,
        FailureReason = reason
    };
}
