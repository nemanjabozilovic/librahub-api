using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Errors;
using LibraHub.Orders.Domain.Orders;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;
using PricingQuote = LibraHub.Orders.Application.Abstractions.PricingQuote;
using PricingQuoteItem = LibraHub.Orders.Application.Abstractions.PricingQuoteItem;

namespace LibraHub.Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderHandler(
    IOrderRepository orderRepository,
    ICatalogPricingClient catalogClient,
    ILibraryOwnershipClient libraryClient,
    IIdentityClient identityClient,
    ICurrentUser currentUser,
    IOutboxWriter outboxWriter,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(OrdersErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return userIdResult;
        }

        var userId = userIdResult.Value;

        var userValidationResult = await ValidateUserAsync(identityClient, userId, cancellationToken);
        if (userValidationResult.IsFailure)
        {
            return userValidationResult;
        }

        var pricingQuoteResult = await catalogClient.GetPricingQuoteAsync(request.BookIds, userId, cancellationToken);
        if (pricingQuoteResult.IsFailure)
        {
            return Result.Failure<Guid>(pricingQuoteResult.Error ?? Error.Unexpected("Failed to retrieve pricing quote"));
        }

        var pricingQuote = pricingQuoteResult.Value;

        var pricingValidationResult = ValidatePricingQuote(pricingQuote, request.BookIds.Count);
        if (pricingValidationResult.IsFailure)
        {
            return pricingValidationResult;
        }

        var bookValidationResult = ValidateBooks(pricingQuote!);
        if (bookValidationResult.IsFailure)
        {
            return bookValidationResult;
        }

        var ownershipValidationResult = await ValidateBookOwnershipAsync(libraryClient, userId, request.BookIds, cancellationToken);
        if (ownershipValidationResult.IsFailure)
        {
            return ownershipValidationResult;
        }

        var order = CreateOrderFromQuote(userId, pricingQuote);

        return await SaveOrderWithTransactionAsync(order, cancellationToken);
    }

    private async Task<Result<Guid>> ValidateUserAsync(IIdentityClient identityClient, Guid userId, CancellationToken cancellationToken)
    {
        var userInfoResult = await identityClient.GetUserInfoAsync(userId, cancellationToken);
        if (userInfoResult.IsFailure)
        {
            return Result.Failure<Guid>(userInfoResult.Error ?? Error.NotFound("User"));
        }

        var userInfo = userInfoResult.Value;

        if (!userInfo.IsActive)
        {
            return Result.Failure<Guid>(Error.Validation(OrdersErrors.User.NotActive));
        }

        if (!userInfo.IsEmailVerified)
        {
            return Result.Failure<Guid>(Error.Validation(OrdersErrors.User.EmailNotVerified));
        }

        return Result.Success(Guid.Empty);
    }

    private Result<Guid> ValidatePricingQuote(PricingQuote? pricingQuote, int expectedCount)
    {
        if (pricingQuote == null || pricingQuote.Items.Count != expectedCount)
        {
            return Result.Failure<Guid>(Error.NotFound("Could not retrieve pricing information for all books"));
        }

        return Result.Success(Guid.Empty);
    }

    private Result<Guid> ValidateBooks(PricingQuote pricingQuote)
    {
        foreach (var item in pricingQuote.Items)
        {
            if (!item.IsPublished)
            {
                return Result.Failure<Guid>(Error.Validation(OrdersErrors.Book.NotPublished));
            }

            if (item.IsRemoved)
            {
                return Result.Failure<Guid>(Error.Validation(OrdersErrors.Book.Removed));
            }

            if (item.FinalPrice == 0)
            {
                return Result.Failure<Guid>(Error.Validation(OrdersErrors.Book.IsFree));
            }
        }

        return Result.Success(Guid.Empty);
    }

    private async Task<Result<Guid>> ValidateBookOwnershipAsync(ILibraryOwnershipClient libraryClient, Guid userId, List<Guid> bookIds, CancellationToken cancellationToken)
    {
        var ownedBookIdsResult = await libraryClient.GetOwnedBookIdsAsync(userId, bookIds, cancellationToken);
        if (ownedBookIdsResult.IsFailure)
        {
            return Result.Failure<Guid>(ownedBookIdsResult.Error ?? Error.Unexpected("Failed to validate book ownership"));
        }

        var ownedBookIds = ownedBookIdsResult.Value;
        if (ownedBookIds.Count > 0)
        {
            return Result.Failure<Guid>(Error.Validation(OrdersErrors.Book.AlreadyOwned));
        }

        return Result.Success(Guid.Empty);
    }

    private Order CreateOrderFromQuote(Guid userId, PricingQuote pricingQuote)
    {
        var orderId = Guid.NewGuid();
        var orderItems = new List<OrderItem>();
        var currency = pricingQuote.Currency;
        var subtotal = Money.Zero(currency);
        var vatTotal = Money.Zero(currency);

        foreach (var quoteItem in pricingQuote.Items)
        {
            var orderItem = CreateOrderItem(orderId, quoteItem, currency);
            orderItems.Add(orderItem);
            subtotal = subtotal.Add(orderItem.FinalPrice);
            vatTotal = vatTotal.Add(orderItem.VatAmount);
        }

        return new Order(orderId, userId, orderItems, subtotal, vatTotal, subtotal.Add(vatTotal));
    }

    private OrderItem CreateOrderItem(Guid orderId, PricingQuoteItem quoteItem, string currency)
    {
        var basePrice = new Money(quoteItem.BasePrice, currency);
        var finalPrice = new Money(quoteItem.FinalPrice, currency);
        var vatAmount = new Money(quoteItem.FinalPrice * (quoteItem.VatRate / 100m), currency);

        return new OrderItem(
            Guid.NewGuid(),
            orderId,
            quoteItem.BookId,
            quoteItem.BookTitle,
            basePrice,
            finalPrice,
            quoteItem.VatRate,
            vatAmount,
            quoteItem.PromotionId,
            quoteItem.PromotionName,
            quoteItem.DiscountAmount);
    }

    private async Task<Result<Guid>> SaveOrderWithTransactionAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await orderRepository.AddAsync(order, ct);

                await outboxWriter.WriteAsync(
                    CreateOrderCreatedEvent(order),
                    Contracts.Common.EventTypes.OrderCreated,
                    ct);
            }, cancellationToken);

            return Result.Success(order.Id);
        }
        catch
        {
            throw;
        }
    }

    private Contracts.Orders.V1.OrderCreatedV1 CreateOrderCreatedEvent(Order order)
    {
        return new Contracts.Orders.V1.OrderCreatedV1
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Items = order.Items.Select(i => new Contracts.Orders.V1.OrderItemDto
            {
                BookId = i.BookId,
                BookTitle = i.BookTitle,
                BasePrice = i.BasePrice.Amount,
                FinalPrice = i.FinalPrice.Amount,
                VatRate = i.VatRate,
                VatAmount = i.VatAmount.Amount,
                PromotionId = i.PromotionId,
                PromotionName = i.PromotionName,
                DiscountAmount = i.DiscountAmount
            }).ToList(),
            Subtotal = order.Subtotal.Amount,
            VatTotal = order.VatTotal.Amount,
            Total = order.Total.Amount,
            Currency = order.Currency,
            CreatedAt = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero)
        };
    }
}
