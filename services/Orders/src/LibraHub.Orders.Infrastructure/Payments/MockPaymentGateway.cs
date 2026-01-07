using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using LibraHub.Orders.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LibraHub.Orders.Infrastructure.Payments;

public class MockPaymentGateway : IPaymentGateway
{
    private readonly MockPaymentOptions _options;
    private readonly Random _random = new();

    public MockPaymentGateway(IOptions<MockPaymentOptions> options)
    {
        _options = options.Value;
    }

    public async Task<PaymentResult> InitiatePaymentAsync(
        Guid orderId,
        Money amount,
        PaymentProvider provider,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        if (ShouldFailPayment(amount))
        {
            var failureReason = GetRandomFailureReason();
            return PaymentResult.Failed(failureReason);
        }

        var providerReference = $"mock_{orderId}_{Guid.NewGuid():N}";
        return PaymentResult.Succeeded(providerReference);
    }

    public async Task<PaymentResult> CapturePaymentAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        if (ShouldFailPayment(null))
        {
            var failureReason = GetRandomFailureReason();
            return PaymentResult.Failed(failureReason);
        }

        return PaymentResult.Succeeded(providerReference);
    }

    private bool ShouldFailPayment(Money? amount)
    {
        if (_options.UseAmountBasedFailure && amount != null)
        {
            var amountString = amount.Amount.ToString("F2");
            var lastTwoDigits = amountString.Length >= 2
                ? amountString.Substring(amountString.Length - 2)
                : amountString;

            if (_options.FailureAmountEndings.Contains(lastTwoDigits))
            {
                return true;
            }
        }

        if (_options.FailureProbabilityPercent > 0)
        {
            var randomValue = _random.Next(0, 100);
            if (randomValue < _options.FailureProbabilityPercent)
            {
                return true;
            }
        }

        return false;
    }

    private string GetRandomFailureReason()
    {
        if (_options.FailureReasons.Count == 0)
        {
            return "Payment failed";
        }

        var index = _random.Next(0, _options.FailureReasons.Count);
        return _options.FailureReasons[index];
    }
}
