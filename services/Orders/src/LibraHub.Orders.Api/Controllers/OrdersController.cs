using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Api.Dtos.Orders;
using LibraHub.Orders.Api.Dtos.Payments;
using LibraHub.Orders.Application.Orders.Commands.CancelOrder;
using LibraHub.Orders.Application.Orders.Commands.CapturePayment;
using LibraHub.Orders.Application.Orders.Commands.CreateOrder;
using LibraHub.Orders.Application.Orders.Commands.StartPayment;
using LibraHub.Orders.Application.Orders.Queries.GetMyOrders;
using LibraHub.Orders.Application.Orders.Queries.GetOrder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Error = LibraHub.BuildingBlocks.Results.Error;
using OrderDto = LibraHub.Orders.Application.Orders.Queries.GetOrder.OrderDto;

namespace LibraHub.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand
        {
            BookIds = request.BookIds
        };

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetOrder), new { orderId = result.Value }, new { orderId = result.Value });
        }

        return result.ToActionResult(this);
    }

    [HttpPost("{orderId}/start-payment")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartPayment(
        Guid orderId,
        [FromBody] StartPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new StartPaymentCommand
        {
            OrderId = orderId,
            Provider = request.Provider
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("{orderId}/capture-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CapturePayment(
        Guid orderId,
        [FromBody] CapturePaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CapturePaymentCommand
        {
            OrderId = orderId,
            PaymentId = request.PaymentId,
            ProviderReference = request.ProviderReference
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("{orderId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new CancelOrderCommand
        {
            OrderId = orderId,
            Reason = request?.Reason
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderQuery { OrderId = orderId };
        var result = await mediator.Send(query, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetMyOrdersResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyOrdersQuery
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, cancellationToken);

        return result.ToActionResult(this);
    }
}
