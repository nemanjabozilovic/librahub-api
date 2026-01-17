namespace LibraHub.Orders.Api.Dtos.Payments;

public record PaymentProvidersResponseDto
{
    public List<PaymentProviderDto> Providers { get; init; } = new();
}

public record PaymentProviderDto
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Type { get; init; } = "Mock";
    public bool IsMocked { get; init; } = true;
}
