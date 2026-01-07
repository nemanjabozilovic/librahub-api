using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Upload.Commands.UploadCover;
using LibraHub.Content.Application.Upload.Commands.UploadEdition;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Content.Api.Controllers;

[ApiController]
[Route("api/books/{bookId}")]
[Authorize(Roles = "Librarian,Admin")]
public class UploadController(IMediator mediator) : ControllerBase
{
    [HttpPost("cover")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadCover(
        Guid bookId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadCoverCommand(bookId, file);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("editions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadEdition(
        Guid bookId,
        IFormFile file,
        [FromForm] string format,
        CancellationToken cancellationToken = default)
    {
        var command = new UploadEditionCommand(bookId, file, format);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }
}
