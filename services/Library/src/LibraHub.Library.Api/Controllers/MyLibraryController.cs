using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Api.Dtos.Reading;
using LibraHub.Library.Application.Entitlements.Queries.MyBooks;
using LibraHub.Library.Application.Reading.Commands.UpdateProgress;
using LibraHub.Library.Application.Reading.Queries.GetProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Library.Api.Controllers;

[ApiController]
[Route("api/my")]
[Authorize]
public class MyLibraryController(IMediator mediator) : ControllerBase
{
    [HttpGet("books")]
    [ProducesResponseType(typeof(MyBooksDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBooks(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new MyBooksQuery
        {
            Skip = skip,
            Take = take
        };

        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("books/{bookId}/progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProgress(
        Guid bookId,
        [FromBody] UpdateProgressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateProgressCommand
        {
            BookId = bookId,
            Format = request.Format,
            Version = request.Version,
            Percentage = request.Percentage,
            LastPage = request.LastPage
        };

        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("books/{bookId}/progress")]
    [ProducesResponseType(typeof(ReadingProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProgress(
        Guid bookId,
        [FromQuery] string? format = null,
        [FromQuery] int? version = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProgressQuery
        {
            BookId = bookId,
            Format = format,
            Version = version
        };

        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
