using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Queries.GetAllOrders;
using LibraHub.Orders.Domain.Orders;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Queries.GetAllOrders;

public class GetAllOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IIdentityClient> _identityClient = new();

    private readonly Guid _userId = Guid.NewGuid();

    private GetAllOrdersHandler CreateHandler() => new(_orderRepository.Object, _identityClient.Object);

    private Order CreateOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        return new Order(Guid.NewGuid(), _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
    }

    [Fact]
    public async Task Handle_InvalidPage_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetAllOrdersQuery { Page = 0, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidPageSize_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetAllOrdersQuery { Page = 1, PageSize = 0 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_Valid_ReturnsOrdersWithUserInfo()
    {
        _orderRepository
            .Setup(r => r.GetAllAsync(0, 20, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { CreateOrder() });
        _orderRepository
            .Setup(r => r.CountAllAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _identityClient
            .Setup(c => c.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success(new Dictionary<Guid, UserInfo?>
            {
                [_userId] = new UserInfo { Id = _userId, Email = "u@x.com", DisplayName = "User" }
            }));

        var result = await CreateHandler().Handle(new GetAllOrdersQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Orders);
        Assert.Equal("User", result.Value.Orders[0].UserDisplayName);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_PeriodFilter_PassesFromDateToRepository()
    {
        _orderRepository
            .Setup(r => r.GetAllAsync(0, 20, It.Is<DateTime?>(d => d.HasValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        _orderRepository
            .Setup(r => r.CountAllAsync(It.Is<DateTime?>(d => d.HasValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _identityClient
            .Setup(c => c.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success(new Dictionary<Guid, UserInfo?>()));

        var result = await CreateHandler().Handle(new GetAllOrdersQuery { Page = 1, PageSize = 20, Period = "7d" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _orderRepository.Verify(r => r.GetAllAsync(0, 20, It.Is<DateTime?>(d => d.HasValue), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserInfoLookupFails_StillReturnsOrdersWithoutUserInfo()
    {
        _orderRepository
            .Setup(r => r.GetAllAsync(0, 20, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { CreateOrder() });
        _orderRepository
            .Setup(r => r.CountAllAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _identityClient
            .Setup(c => c.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Failure<Dictionary<Guid, UserInfo?>>(BuildingBlocks.Results.Error.Unexpected("boom")));

        var result = await CreateHandler().Handle(new GetAllOrdersQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Orders);
        Assert.Null(result.Value.Orders[0].UserDisplayName);
    }
}
