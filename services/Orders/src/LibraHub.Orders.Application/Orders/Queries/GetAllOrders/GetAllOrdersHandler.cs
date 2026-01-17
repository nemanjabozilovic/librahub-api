using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Application.Orders.Queries.GetAllOrders;

public class GetAllOrdersHandler(
    IOrderRepository orderRepository,
    IIdentityClient identityClient) : IRequestHandler<GetAllOrdersQuery, Result<GetAllOrdersResponseDto>>
{
    public async Task<Result<GetAllOrdersResponseDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1)
        {
            return Result.Failure<GetAllOrdersResponseDto>(Error.Validation("Page must be greater than 0"));
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result.Failure<GetAllOrdersResponseDto>(Error.Validation("PageSize must be between 1 and 100"));
        }

        var fromDate = ParsePeriod(request.Period);

        var skip = (request.Page - 1) * request.PageSize;
        var orders = await orderRepository.GetAllAsync(skip, request.PageSize, fromDate, cancellationToken);
        var totalCount = await orderRepository.CountAllAsync(fromDate, cancellationToken);

        var uniqueUserIds = orders.Select(o => o.UserId).Distinct().ToList();
        var userInfoDictResult = await identityClient.GetUsersByIdsAsync(uniqueUserIds, cancellationToken);
        var userInfoDict = userInfoDictResult.IsSuccess
            ? userInfoDictResult.Value
            : new Dictionary<Guid, UserInfo?>();

        var response = new GetAllOrdersResponseDto
        {
            Orders = orders.Select(o =>
            {
                var userInfo = userInfoDict.GetValueOrDefault(o.UserId);
                return new AdminOrderSummaryDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    UserDisplayName = userInfo?.DisplayName,
                    UserEmail = userInfo?.Email,
                    Status = o.Status.ToString(),
                    Total = o.Total.Amount,
                    Currency = o.Currency,
                    CreatedAt = new DateTimeOffset(o.CreatedAt, TimeSpan.Zero),
                    ItemCount = o.Items.Count
                };
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }

    private static DateTime? ParsePeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            return null;
        }

        return period.ToLower() switch
        {
            "24h" => DateTime.UtcNow.AddHours(-24),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => null
        };
    }
}
