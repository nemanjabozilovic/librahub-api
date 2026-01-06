using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Api.Dtos.Entitlements;
using LibraHub.Library.Application.Entitlements.Commands.AdminGrantEntitlement;
using LibraHub.Library.Application.Entitlements.Commands.RevokeEntitlement;
using LibraHub.Library.Application.Entitlements.Queries.GetAllEntitlements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Library.Api.Controllers;

[ApiController]
[Route("api/admin/entitlements")]
[Authorize(Roles = "Admin")]
public class AdminEntitlementsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetAllEntitlementsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllEntitlements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? bookId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] string? period = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllEntitlementsQuery
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            BookId = bookId,
            Status = status,
            Source = source,
            Period = period
        };

        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("grant")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GrantEntitlement(
        [FromBody] GrantEntitlementRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new AdminGrantEntitlementCommand
        {
            UserId = request.UserId,
            BookId = request.BookId
        };

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GrantEntitlement), new { id = result.Value }, new { id = result.Value });
        }

        return result.ToActionResult(this);
    }

    [HttpPost("{id}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeEntitlement(
        Guid id,
        [FromBody] RevokeEntitlementRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new RevokeEntitlementCommand
        {
            EntitlementId = id,
            Reason = request?.Reason
        };

        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

