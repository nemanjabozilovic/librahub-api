using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.UpdateBook;

public record UpdateBookCommand(
    Guid BookId,
    string? Title,
    string? Description,
    string? Language,
    string? Publisher,
    DateTimeOffset? PublicationDate,
    string? Isbn,
    List<string>? Authors,
    List<string>? Categories,
    List<string>? Tags) : IRequest<Result>;
