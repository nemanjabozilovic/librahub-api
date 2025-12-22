namespace LibraHub.Catalog.Domain.Books;

public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    private Money()
    { } // For EF Core

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency) => new(0, currency);

    public bool IsFree => Amount == 0;
}
