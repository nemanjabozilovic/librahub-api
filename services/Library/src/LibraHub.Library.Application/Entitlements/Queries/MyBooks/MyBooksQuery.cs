using MediatR;

namespace LibraHub.Library.Application.Entitlements.Queries.MyBooks;

public class MyBooksQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<MyBooksDto>>
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 20;
}
