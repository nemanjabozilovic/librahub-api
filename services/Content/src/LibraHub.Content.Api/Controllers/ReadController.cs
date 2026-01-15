using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Access.Commands.CreateReadToken;
using LibraHub.Content.Application.Access.Queries.ValidateReadToken;
using LibraHub.Content.Application.Covers.Queries.GetBookCover;
using LibraHub.Content.Application.Editions.Queries.GetBookEditions;
using LibraHub.Content.Application.Editions.Queries.GetBookEditionsBatch;
using LibraHub.Content.Application.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Api.Controllers;

[ApiController]
[Route("api")]
public class ReadController(
    IMediator mediator,
    IObjectStorage objectStorage,
    IOptions<UploadOptions> uploadOptions) : ControllerBase
{
    [HttpGet("books/{bookId}/cover")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BookCoverDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookCover(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var query = new Application.Covers.Queries.GetBookCover.GetBookCoverQuery(bookId);
        var result = await mediator.Send(query, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("books/{bookId}/editions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<BookEditionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookEditions(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBookEditionsQuery(bookId);
        var result = await mediator.Send(query, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("books/editions/batch")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Dictionary<Guid, List<BookEditionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookEditionsBatch(
        [FromBody] GetBookEditionsBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBookEditionsBatchQuery(request.BookIds ?? new List<Guid>());
        var result = await mediator.Send(query, cancellationToken);

        return result.ToActionResult(this);
    }

    public record GetBookEditionsBatchRequest
    {
        public List<Guid>? BookIds { get; init; }
    }

    [HttpPost("books/{bookId}/read-token")]
    [Authorize]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReadToken(
        Guid bookId,
        [FromQuery] string? format = null,
        [FromQuery] int? version = null,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateReadTokenCommand(bookId, format, version);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { token = result.Value });
        }

        return result.ToActionResult(this);
    }

    [HttpGet("stream")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StreamContent(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        var query = new ValidateReadTokenQuery(token);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToActionResult(this);
        }

        var grantInfo = result.Value;

        var bucketName = grantInfo.Scope == "Cover"
            ? uploadOptions.Value.CoversBucketName
            : uploadOptions.Value.EditionsBucketName;

        Stream? stream = null;
        try
        {
            stream = await objectStorage.DownloadAsync(
                bucketName,
                grantInfo.ObjectKey,
                cancellationToken);

            return File(stream, grantInfo.ContentType, enableRangeProcessing: true);
        }
        catch (ObjectDisposedException)
        {
            stream?.Dispose();
            return StatusCode(500, new Error("STORAGE_ERROR", "Stream was disposed unexpectedly"));
        }
        catch
        {
            stream?.Dispose();
            return StatusCode(500, new Error("STORAGE_ERROR", "Failed to download content"));
        }
    }
}
