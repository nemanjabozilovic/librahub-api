using LibraHub.BuildingBlocks.Persistence;

namespace LibraHub.Identity.Infrastructure.Persistence;

public class UnitOfWork(IdentityDbContext context) : UnitOfWork<IdentityDbContext>(context)
{
}
