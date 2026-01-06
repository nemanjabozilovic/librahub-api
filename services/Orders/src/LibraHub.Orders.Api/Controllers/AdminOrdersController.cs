using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Api.Dtos.Refunds;
using LibraHub.Orders.Application.Orders.Commands.RefundOrder;
using LibraHub.Orders.Application.Orders.Queries.GetAllOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetAllOrdersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? period = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllOrdersQuery
        {
            Page = page,
            PageSize = pageSize,
            Period = period
        };

        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{orderId}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundOrder(
        Guid orderId,
        [FromBody] RefundOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RefundOrderCommand
        {
            OrderId = orderId,
            Reason = request.Reason
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }
}
