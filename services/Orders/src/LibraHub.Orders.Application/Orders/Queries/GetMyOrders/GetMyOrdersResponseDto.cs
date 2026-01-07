namespace LibraHub.Orders.Application.Orders.Queries.GetMyOrders;

public class GetMyOrdersResponseDto
{
    public List<OrderSummaryDto> Orders { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class OrderSummaryDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public int ItemCount { get; init; }
}
