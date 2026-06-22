using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Queries.GetMyOrders;
using LibraHub.Orders.Domain.Orders;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Queries.GetMyOrders;

public class GetMyOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();

    public GetMyOrdersHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
    }

    private GetMyOrdersHandler CreateHandler() => new(_orderRepository.Object, _currentUser.Object);

    private Order CreateOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        return new Order(Guid.NewGuid(), _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
    }

    [Fact]
    public async Task Handle_UserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMyOrdersQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidPage_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetMyOrdersQuery { Page = 0, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidPageSize_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetMyOrdersQuery { Page = 1, PageSize = 101 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_Valid_ReturnsPagedOrders()
    {
        _orderRepository
            .Setup(r => r.GetByUserIdAsync(_userId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { CreateOrder() });
        _orderRepository
            .Setup(r => r.CountByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateHandler().Handle(new GetMyOrdersQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Orders);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Orders[0].ItemCount);
    }
}
