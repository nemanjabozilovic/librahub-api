using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Domain.Access;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Content.Application.Access.Commands.CreateReadToken;

public class CreateReadTokenHandler(
    IAccessGrantRepository accessGrantRepository,
    ICatalogReadClient catalogClient,
    ILibraryAccessClient libraryClient,
    IBookEditionRepository editionRepository,
    ICoverRepository coverRepository,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<ReadAccessOptions> readAccessOptions) : IRequestHandler<CreateReadTokenCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateReadTokenCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId("User not authenticated");
        if (userIdResult.IsFailure)
        {
            return Result.Failure<string>(userIdResult.Error!);
        }

        var userId = userIdResult.Value;

        var bookInfo = await catalogClient.GetBookInfoAsync(request.BookId, cancellationToken);
        if (bookInfo == null)
        {
            return Result.Failure<string>(Error.NotFound(ContentErrors.Book.NotFound));
        }

        if (bookInfo.IsBlocked)
        {
            return Result.Failure<string>(Error.Validation(ContentErrors.Book.Blocked));
        }

        bool hasAccess = currentUser.IsInRole("Admin") || currentUser.IsInRole("Librarian");
        if (!hasAccess)
        {
            hasAccess = bookInfo.IsFree;
            if (!hasAccess)
            {
                hasAccess = await libraryClient.UserOwnsBookAsync(userId, request.BookId, cancellationToken);
            }
        }

        if (!hasAccess)
        {
            return Result.Failure<string>(Error.Forbidden(ContentErrors.Access.AccessDenied));
        }

        AccessScope scope;
        BookFormat? format = null;
        int? version = null;

        if (string.IsNullOrEmpty(request.Format))
        {
            var cover = await coverRepository.GetByBookIdAsync(request.BookId, cancellationToken);
            if (cover == null || !cover.IsAccessible)
            {
                return Result.Failure<string>(Error.NotFound(ContentErrors.Cover.NotFound));
            }
            scope = AccessScope.Cover;
        }
        else
        {
            if (!Enum.TryParse<BookFormat>(request.Format, ignoreCase: true, out var parsedFormat))
            {
                return Result.Failure<string>(Error.Validation(ContentErrors.Edition.InvalidFormat));
            }

            format = parsedFormat;

            if (request.Version.HasValue)
            {
                version = request.Version.Value;
                var edition = await editionRepository.GetByBookIdFormatAndVersionAsync(
                    request.BookId, format.Value, version.Value, cancellationToken);
                if (edition == null || !edition.IsAccessible)
                {
                    return Result.Failure<string>(Error.NotFound(ContentErrors.Edition.NotFound));
                }
            }
            else
            {
                var latest = await editionRepository.GetLatestByBookIdAndFormatAsync(
                    request.BookId, format.Value, cancellationToken);
                if (latest == null || !latest.IsAccessible)
                {
                    return Result.Failure<string>(Error.NotFound(ContentErrors.Edition.NotFound));
                }
                version = latest.Version;
            }

            scope = AccessScope.Edition;
        }

        var token = GenerateSecureToken();
        var expiresAt = clock.UtcNow.AddMinutes(readAccessOptions.Value.TokenExpirationMinutes);
        var grant = new AccessGrant(
            Guid.NewGuid(),
            token,
            request.BookId,
            format,
            version,
            scope,
            userId,
            expiresAt);

        await accessGrantRepository.AddAsync(grant, cancellationToken);

        return Result.Success(token);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
