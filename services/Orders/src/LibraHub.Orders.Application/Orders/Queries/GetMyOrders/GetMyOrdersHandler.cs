using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Application.Orders.Queries.GetMyOrders;

public class GetMyOrdersHandler(
    IOrderRepository orderRepository,
    ICurrentUser currentUser) : IRequestHandler<GetMyOrdersQuery, Result<GetMyOrdersResponseDto>>
{
    public async Task<Result<GetMyOrdersResponseDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(OrdersErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure<GetMyOrdersResponseDto>(userIdResult.Error!);
        }

        var userId = userIdResult.Value;

        if (request.Page < 1)
        {
            return Result.Failure<GetMyOrdersResponseDto>(Error.Validation("Page must be greater than 0"));
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result.Failure<GetMyOrdersResponseDto>(Error.Validation("PageSize must be between 1 and 100"));
        }

        var skip = (request.Page - 1) * request.PageSize;
        var orders = await orderRepository.GetByUserIdAsync(userId, skip, request.PageSize, cancellationToken);
        var totalCount = await orderRepository.CountByUserIdAsync(userId, cancellationToken);

        var response = new GetMyOrdersResponseDto
        {
            Orders = orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                Total = o.Total.Amount,
                Currency = o.Currency,
                CreatedAt = new DateTimeOffset(o.CreatedAt, TimeSpan.Zero),
                ItemCount = o.Items.Count
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
