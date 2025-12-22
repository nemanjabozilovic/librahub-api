using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.CreateBook;

public record CreateBookCommand(string Title) : IRequest<Result<Guid>>;
