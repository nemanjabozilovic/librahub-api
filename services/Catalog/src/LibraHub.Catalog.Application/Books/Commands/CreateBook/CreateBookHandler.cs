using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookHandler(
    IBookRepository bookRepository,
    IOutboxWriter outboxWriter) : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new Book(Guid.NewGuid(), request.Title);

        var isbnResult = TryCreateIsbn(request.Isbn);
        if (isbnResult.IsFailure)
        {
            return Result.Failure<Guid>(isbnResult.Error!);
        }

        var isbn = isbnResult.Value;

        book.UpdateMetadata(
            title: null,
            request.Description,
            request.Language,
            request.Publisher,
            request.PublicationDate.UtcDateTime,
            isbn);

        foreach (var author in request.Authors)
        {
            book.AddAuthor(author);
        }

        foreach (var category in request.Categories)
        {
            book.AddCategory(category);
        }

        if (request.Tags != null)
        {
            foreach (var tag in request.Tags)
            {
                book.AddTag(tag);
            }
        }

        await bookRepository.AddAsync(book, cancellationToken);

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookCreatedV1
            {
                BookId = book.Id,
                Title = book.Title,
                CreatedAt = new DateTimeOffset(book.CreatedAt, TimeSpan.Zero)
            },
            Contracts.Common.EventTypes.BookCreated,
            cancellationToken);

        return Result.Success(book.Id);
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
}
