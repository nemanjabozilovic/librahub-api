using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookHandler(
    IBookRepository bookRepository,
    IOutboxWriter outboxWriter) : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new Book(Guid.NewGuid(), request.Title);

        await bookRepository.AddAsync(book, cancellationToken);

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookCreatedV1
            {
                BookId = book.Id,
                Title = book.Title,
                CreatedAt = book.CreatedAt
            },
            Contracts.Common.EventTypes.BookCreated,
            cancellationToken);

        return Result.Success(book.Id);
    }
}
