using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.UpdateBook;

public class UpdateBookHandler(
    IBookRepository bookRepository,
    IOutboxWriter outboxWriter,
    ICache cache) : IRequestHandler<UpdateBookCommand, Result>
{
    public async Task<Result> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        Isbn? isbn = null;
        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            var isbnResult = TryCreateIsbn(request.Isbn);
            if (isbnResult.IsFailure)
            {
                return Result.Failure(isbnResult.Error!);
            }
            isbn = isbnResult.Value;
        }

        book.UpdateMetadata(
            request.Title,
            request.Description,
            request.Language,
            request.Publisher,
            request.PublicationDate?.UtcDateTime,
            isbn);

        if (request.Authors != null)
        {
            UpdateAuthors(book, request.Authors);
        }

        if (request.Categories != null)
        {
            UpdateCategories(book, request.Categories);
        }

        if (request.Tags != null)
        {
            UpdateTags(book, request.Tags);
        }

        await bookRepository.UpdateAsync(book, cancellationToken);

        var authors = string.Join(", ", book.Authors.Select(a => a.Name));

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookUpdatedV1
            {
                BookId = book.Id,
                Title = book.Title,
                Authors = authors,
                UpdatedAt = new DateTimeOffset(book.UpdatedAt, TimeSpan.Zero)
            },
            Contracts.Common.EventTypes.BookUpdated,
            cancellationToken);

        await CacheInvalidationHelper.InvalidateBookCacheAsync(cache, book.Id, cancellationToken);

        return Result.Success();
    }

    private static Result<Isbn> TryCreateIsbn(string isbnValue)
    {
        try
        {
            var isbn = new Isbn(isbnValue);
            return Result.Success(isbn);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Isbn>(Error.Validation("Invalid ISBN format"));
        }
    }

    private static void UpdateAuthors(Book book, List<string> newAuthors)
    {
        var existingAuthors = book.Authors.Select(a => a.Name).ToList();

        foreach (var author in newAuthors)
        {
            if (!existingAuthors.Contains(author))
            {
                book.AddAuthor(author);
            }
        }

        foreach (var existingAuthor in existingAuthors)
        {
            if (!newAuthors.Contains(existingAuthor))
            {
                book.RemoveAuthor(existingAuthor);
            }
        }
    }

    private static void UpdateCategories(Book book, List<string> newCategories)
    {
        var existingCategories = book.Categories.Select(c => c.Name).ToList();

        foreach (var category in newCategories)
        {
            if (!existingCategories.Contains(category))
            {
                book.AddCategory(category);
            }
        }

        foreach (var existingCategory in existingCategories)
        {
            if (!newCategories.Contains(existingCategory))
            {
                book.RemoveCategory(existingCategory);
            }
        }
    }

    private static void UpdateTags(Book book, List<string> newTags)
    {
        var existingTags = book.Tags.Select(t => t.Name).ToList();

        foreach (var tag in newTags)
        {
            if (!existingTags.Contains(tag))
            {
                book.AddTag(tag);
            }
        }

        foreach (var existingTag in existingTags)
        {
            if (!newTags.Contains(existingTag))
            {
                book.RemoveTag(existingTag);
            }
        }
    }
}
