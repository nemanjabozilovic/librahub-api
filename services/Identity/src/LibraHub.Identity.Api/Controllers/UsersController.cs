using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Api.Dtos.Users;
using LibraHub.Identity.Application.Admin.Commands.AssignRole;
using LibraHub.Identity.Application.Admin.Commands.RemoveUser;
using LibraHub.Identity.Application.Users.Commands.CompleteRegistration;
using LibraHub.Identity.Application.Users.Commands.CreateUser;
using LibraHub.Identity.Application.Users.Commands.UpdateUser;
using LibraHub.Identity.Application.Users.Commands.UploadAvatar;
using LibraHub.Identity.Application.Users.Queries.GetRemovedUsers;
using LibraHub.Identity.Application.Users.Queries.GetUser;
using LibraHub.Identity.Application.Users.Queries.GetUsers;
using LibraHub.Identity.Application.Users.Queries.GetUsersByIds;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetUsersResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(skip, take);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("removed")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetRemovedUsersResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRemovedUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRemovedUsersQuery(skip, take);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}/info")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserInfo(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("by-ids")]
    [Authorize(Policy = "InternalAccess")]
    [ProducesResponseType(typeof(GetUsersByIdsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsersByIds(
        [FromBody] GetUsersByIdsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersByIdsQuery(request.UserIds);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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

    [HttpPost("{id}/remove")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUser(
        [FromRoute] Guid id,
        [FromBody] RemoveUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RemoveUserCommand(id, request.Reason);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LibraHub.Identity.Application.Users.Queries.GetUsers.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            id,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Phone,
            request.EmailVerified);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
        var decodedToken = Uri.UnescapeDataString(request.Token);
        var command = new CompleteRegistrationCommand(
            decodedToken,
            request.Password,
            request.ConfirmPassword,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.EmailAnnouncementsEnabled,
            request.EmailPromotionsEnabled,
            request.Phone);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/avatar")]
    [Authorize(Roles = "Admin")]
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
