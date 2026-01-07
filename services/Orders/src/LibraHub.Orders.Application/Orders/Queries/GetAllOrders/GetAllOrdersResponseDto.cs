namespace LibraHub.Orders.Application.Orders.Queries.GetAllOrders;

public class GetAllOrdersResponseDto
{
    public List<AdminOrderSummaryDto> Orders { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class AdminOrderSummaryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? UserDisplayName { get; init; }
    public string? UserEmail { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public int ItemCount { get; init; }
}
