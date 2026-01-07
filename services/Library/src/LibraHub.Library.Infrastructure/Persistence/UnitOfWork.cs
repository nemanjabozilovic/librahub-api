using LibraHub.BuildingBlocks.Persistence;

namespace LibraHub.Library.Infrastructure.Persistence;

public class UnitOfWork(LibraryDbContext context) : UnitOfWork<LibraryDbContext>(context)
{
}
