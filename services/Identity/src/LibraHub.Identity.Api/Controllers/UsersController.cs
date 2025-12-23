using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Api.Dtos.Users;
using LibraHub.Identity.Application.Admin.Commands.AssignRole;
using LibraHub.Identity.Application.Admin.Commands.DisableUser;
using LibraHub.Identity.Application.Users.Commands.CompleteRegistration;
using LibraHub.Identity.Application.Users.Commands.CreateUser;
using LibraHub.Identity.Application.Users.Commands.UploadAvatar;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "Admin")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRole(
        [FromRoute] Guid id,
        [FromBody] AssignRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        var command = new AssignRoleCommand(id, role, true);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveRole(
        [FromRoute] Guid id,
        [FromRoute] string role,
        CancellationToken cancellationToken)
    {
        var roleEnum = Enum.Parse<Role>(role, ignoreCase: true);
        var command = new AssignRoleCommand(id, roleEnum, false);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableUser(
        [FromRoute] Guid id,
        [FromBody] DisableUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new DisableUserCommand(id, request.Reason, true);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EnableUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DisableUserCommand(id, string.Empty, false);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        var command = new CreateUserCommand(request.Email, role);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("complete-registration")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteRegistration(
        [FromBody] CompleteRegistrationRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteRegistrationCommand(
            request.Token,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.DateOfBirth);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/avatar")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadAvatarCommand(id, file);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
