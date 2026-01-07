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
            try
            {
                isbn = new Isbn(request.Isbn);
            }
            catch
            {
                return Result.Failure(Error.Validation("Invalid ISBN format"));
            }
        }

        book.UpdateMetadata(
            request.Description,
            request.Language,
            request.Publisher,
            request.PublicationDate?.UtcDateTime,
            isbn);

        if (request.Authors != null)
        {
            var existingAuthors = book.Authors.Select(a => a.Name).ToList();
            foreach (var author in request.Authors)
            {
                if (!existingAuthors.Contains(author))
                {
                    book.AddAuthor(author);
                }
            }
            foreach (var existingAuthor in existingAuthors)
            {
                if (!request.Authors.Contains(existingAuthor))
                {
                    book.RemoveAuthor(existingAuthor);
                }
            }
        }

        if (request.Categories != null)
        {
            var existingCategories = book.Categories.Select(c => c.Name).ToList();
            foreach (var category in request.Categories)
            {
                if (!existingCategories.Contains(category))
                {
                    book.AddCategory(category);
                }
            }
            foreach (var existingCategory in existingCategories)
            {
                if (!request.Categories.Contains(existingCategory))
                {
                    book.RemoveCategory(existingCategory);
                }
            }
        }

        if (request.Tags != null)
        {
            var existingTags = book.Tags.Select(t => t.Name).ToList();
            foreach (var tag in request.Tags)
            {
                if (!existingTags.Contains(tag))
                {
                    book.AddTag(tag);
                }
            }
            foreach (var existingTag in existingTags)
            {
                if (!request.Tags.Contains(existingTag))
                {
                    book.RemoveTag(existingTag);
                }
            }
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
}
