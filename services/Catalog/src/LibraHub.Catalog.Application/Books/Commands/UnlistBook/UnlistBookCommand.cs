using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.UnlistBook;

public record UnlistBookCommand(Guid BookId) : IRequest<Result>;
