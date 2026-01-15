using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Content.Application.Abstractions;

public interface ILibraryAccessClient
{
    Task<Result<bool>> UserOwnsBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
}
