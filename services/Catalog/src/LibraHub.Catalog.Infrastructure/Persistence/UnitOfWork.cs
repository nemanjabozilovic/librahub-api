using LibraHub.BuildingBlocks.Persistence;

namespace LibraHub.Catalog.Infrastructure.Persistence;

public class UnitOfWork(CatalogDbContext context) : UnitOfWork<CatalogDbContext>(context)
{
}
