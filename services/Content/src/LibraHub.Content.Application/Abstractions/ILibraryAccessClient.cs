namespace LibraHub.Content.Application.Abstractions;

public interface ILibraryAccessClient
{
    Task<bool> UserOwnsBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
}
