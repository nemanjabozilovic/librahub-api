using LibraHub.BuildingBlocks.Persistence;

namespace LibraHub.Orders.Infrastructure.Persistence;

public class UnitOfWork(OrdersDbContext context) : UnitOfWork<OrdersDbContext>(context)
{
}
