using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.PublishBook;

public record PublishBookCommand(Guid BookId) : IRequest<Result>;
