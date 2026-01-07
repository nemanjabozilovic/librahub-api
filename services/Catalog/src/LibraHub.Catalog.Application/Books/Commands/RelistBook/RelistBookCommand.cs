using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.RelistBook;

public record RelistBookCommand(Guid BookId) : IRequest<Result>;
