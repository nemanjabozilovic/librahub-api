using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.CreateBook;

public record CreateBookCommand(
    string Title,
    string Description,
    string Language,
    string Publisher,
    DateTimeOffset PublicationDate,
    string Isbn,
    List<string> Authors,
    List<string> Categories,
    List<string>? Tags = null) : IRequest<Result<Guid>>;
