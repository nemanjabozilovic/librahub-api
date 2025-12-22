using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.RemoveBook;

public record RemoveBookCommand(Guid BookId, string Reason) : IRequest<Result>;
