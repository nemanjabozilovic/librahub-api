namespace LibraHub.Orders.Api.Dtos.Orders;

public class CreateOrderRequestDto
{
    public List<Guid> BookIds { get; init; } = new();
}
