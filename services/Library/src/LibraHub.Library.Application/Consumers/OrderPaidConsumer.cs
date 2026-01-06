using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Entitlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;
using System.Net;

namespace LibraHub.Library.Application.Consumers;

public class OrderPaidConsumer(
    IEntitlementRepository entitlementRepository,
    BuildingBlocks.Abstractions.IOutboxWriter outboxWriter,
    BuildingBlocks.Inbox.IInboxRepository inboxRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<OrderPaidConsumer> logger)
{
    private const string EventType = EventTypes.OrderPaid;
    private const int MaxRetryAttempts = 5;

    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<NpgsqlException>(ex => IsTransientPostgresError(ex))
        .Or<DbUpdateException>(ex => ex.InnerException is NpgsqlException npgsqlEx && IsTransientPostgresError(npgsqlEx))
        .Or<HttpRequestException>(ex => IsTransientHttpError(ex))
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: MaxRetryAttempts,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                if (context.TryGetValue("Logger", out var loggerObj) && loggerObj is ILogger<OrderPaidConsumer> retryLogger)
                {
                    var orderId = context.TryGetValue("OrderId", out var orderIdObj)
                        ? orderIdObj?.ToString() ?? "Unknown"
                        : "Unknown";

                    retryLogger.LogWarning(
                        outcome,
                        "Retrying OrderPaid event processing for OrderId: {OrderId}, Attempt: {Attempt}/{MaxAttempts} after {Delay}ms",
                        orderId, retryCount, MaxRetryAttempts, timespan.TotalMilliseconds);
                }
            });

    private static bool IsTransientPostgresError(NpgsqlException ex)
    {
        var transientErrorCodes = new[] { "40001", "40P01", "08000", "08003", "08006", "08007", "57P01", "57P02", "57P03" };
        return transientErrorCodes.Contains(ex.SqlState);
    }

    private static bool IsTransientHttpError(HttpRequestException ex)
    {
        return ex.InnerException is WebException webEx &&
               (webEx.Status == WebExceptionStatus.Timeout ||
                webEx.Status == WebExceptionStatus.ConnectFailure ||
                webEx.Status == WebExceptionStatus.ReceiveFailure);
    }

    public async Task HandleAsync(OrderPaidV1 @event, CancellationToken cancellationToken)
    {
        if (@event == null)
        {
            logger.LogError("Received null OrderPaid event");
            throw new ArgumentNullException(nameof(@event));
        }

        if (@event.OrderId == Guid.Empty)
        {
            logger.LogError("Received OrderPaid event with empty OrderId");
            throw new ArgumentException("OrderId cannot be empty", nameof(@event));
        }

        if (@event.UserId == Guid.Empty)
        {
            logger.LogError("Received OrderPaid event with empty UserId for OrderId: {OrderId}", @event.OrderId);
            throw new ArgumentException("UserId cannot be empty", nameof(@event));
        }

        if (@event.Items == null || @event.Items.Count == 0)
        {
            logger.LogWarning("Received OrderPaid event with no items for OrderId: {OrderId}", @event.OrderId);
            await MarkAsProcessedSafelyAsync(@event.OrderId, cancellationToken);
            return;
        }

        var messageId = $"OrderPaid_{@event.OrderId}";

        logger.LogInformation(
            "Processing OrderPaid event for OrderId: {OrderId}, UserId: {UserId}, Items: {ItemCount}, MessageId: {MessageId}",
            @event.OrderId, @event.UserId, @event.Items.Count, messageId);

        var context = new Context
        {
            { "Logger", logger },
            { "OrderId", @event.OrderId.ToString() }
        };

        try
        {
            await RetryPolicy.ExecuteAsync(async (ctx, ct) =>
            {
                await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
                {
                    if (await inboxRepository.IsProcessedAsync(messageId, transactionCt))
                    {
                        logger.LogInformation(
                            "OrderPaid event already processed for MessageId: {MessageId}, OrderId: {OrderId}",
                            messageId, @event.OrderId);
                        return;
                    }

                    foreach (var item in @event.Items)
                    {
                        if (item == null || item.BookId == Guid.Empty)
                        {
                            logger.LogWarning(
                                "Skipping invalid item in OrderId: {OrderId}, BookId: {BookId}",
                                @event.OrderId, item?.BookId);
                            continue;
                        }

                        var existing = await entitlementRepository.GetByUserAndBookAsync(
                            @event.UserId,
                            item.BookId,
                            transactionCt);

                        if (existing != null)
                        {
                            if (existing.IsActive)
                            {
                                logger.LogInformation(
                                    "Entitlement already exists and is active for UserId: {UserId}, BookId: {BookId}",
                                    @event.UserId, item.BookId);
                            }
                            else
                            {
                                existing.Reactivate();
                                await entitlementRepository.UpdateAsync(existing, transactionCt);

                                logger.LogInformation(
                                    "Reactivated entitlement for UserId: {UserId}, BookId: {BookId}",
                                    @event.UserId, item.BookId);
                            }
                        }
                        else
                        {
                            var entitlement = new Entitlement(
                                Guid.NewGuid(),
                                @event.UserId,
                                item.BookId,
                                EntitlementSource.Purchase,
                                @event.OrderId);

                            await entitlementRepository.AddAsync(entitlement, transactionCt);

                            logger.LogInformation(
                                "Created entitlement for UserId: {UserId}, BookId: {BookId}",
                                @event.UserId, item.BookId);
                        }

                        await outboxWriter.WriteAsync(
                            new EntitlementGrantedV1
                            {
                                UserId = @event.UserId,
                                BookId = item.BookId,
                                Source = EntitlementSource.Purchase.ToString(),
                                AcquiredAtUtc = @event.PaidAt
                            },
                            EventTypes.EntitlementGranted,
                            transactionCt);
                    }

                    await inboxRepository.MarkAsProcessedAsync(messageId, EventType, transactionCt);
                }, ct);
            }, context, cancellationToken);

            logger.LogInformation(
                "Completed processing OrderPaid event for OrderId: {OrderId}, ProcessedItems: {ItemCount}",
                @event.OrderId, @event.Items.Count);
        }
        catch (Exception ex) when (IsRetryableException(ex))
        {
            logger.LogError(ex,
                "Unexpected retryable exception after all retries exhausted for OrderId: {OrderId}. " +
                "This may indicate a configuration issue. Event will be retried by message broker.",
                @event.OrderId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Non-retryable error processing OrderPaid event for OrderId: {OrderId}, initiating automatic refund",
                @event.OrderId);

            await InitiateRefundAsync(@event, ex, cancellationToken);
        }
    }

    private async Task InitiateRefundAsync(OrderPaidV1 @event, Exception originalException, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await outboxWriter.WriteAsync(
                    new OrderRefundedV1
                    {
                        OrderId = @event.OrderId,
                        UserId = @event.UserId,
                        RefundId = Guid.NewGuid(),
                        Reason = "We encountered a technical issue while processing your order. Your payment has been automatically refunded. Please try again later or contact support if the issue persists.",
                        RefundedBy = Guid.Empty,
                        RefundedAt = clock.UtcNow
                    },
                    EventTypes.OrderRefunded,
                    ct);
            }, cancellationToken);

            logger.LogWarning(
                "Initiated automatic refund for OrderId: {OrderId} due to processing failure. Original error: {ErrorType}",
                @event.OrderId, originalException.GetType().Name);
        }
        catch (Exception refundEx)
        {
            logger.LogError(refundEx,
                "Failed to initiate automatic refund for OrderId: {OrderId}. Manual intervention required. " +
                "Original error: {OriginalError}",
                @event.OrderId, originalException.Message);
            throw;
        }
    }

    private async Task MarkAsProcessedSafelyAsync(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var messageId = $"OrderPaid_{orderId}";
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                if (!await inboxRepository.IsProcessedAsync(messageId, ct))
                {
                    await inboxRepository.MarkAsProcessedAsync(messageId, EventType, ct);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark empty order as processed for OrderId: {OrderId}", orderId);
        }
    }

    private static bool IsRetryableException(Exception ex)
    {
        return ex is NpgsqlException npgsqlEx && IsTransientPostgresError(npgsqlEx) ||
               ex is DbUpdateException dbEx && dbEx.InnerException is NpgsqlException innerNpgsqlEx && IsTransientPostgresError(innerNpgsqlEx) ||
               ex is HttpRequestException httpEx && IsTransientHttpError(httpEx) ||
               ex is TimeoutException;
    }
}
