using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Api.Dtos.Auth;
using LibraHub.Identity.Application.Auth.Commands.ForgotPassword;
using LibraHub.Identity.Application.Auth.Commands.Login;
using LibraHub.Identity.Application.Auth.Commands.Refresh;
using LibraHub.Identity.Application.Auth.Commands.Register;
using LibraHub.Identity.Application.Auth.Commands.ResetPassword;
using LibraHub.Identity.Application.Auth.Commands.VerifyEmail;
using LibraHub.Identity.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.DateOfBirth);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RefreshCommand(request.RefreshToken);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request, CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Token);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword, request.ConfirmPassword);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
