using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Api.Dtos.Announcements;
using LibraHub.Catalog.Application.Announcements.Commands.CreateAnnouncement;
using LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;
using LibraHub.Catalog.Application.Announcements.Commands.PublishAnnouncement;
using LibraHub.Catalog.Application.Announcements.Commands.UpdateAnnouncement;
using LibraHub.Catalog.Application.Announcements.Commands.UploadAnnouncementImage;
using LibraHub.Catalog.Application.Announcements.Queries.GetAnnouncements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Api.Controllers;

[ApiController]
[Route("announcements")]
public class AnnouncementsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetAnnouncementsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] Guid? bookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAnnouncementsQuery(bookId, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAnnouncementCommand(request.BookId, request.Title, request.Content);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetAnnouncements), new { id = result.Value });
    }

    [HttpPost("{id}/publish")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishAnnouncement(Guid id, CancellationToken cancellationToken)
    {
        var command = new PublishAnnouncementCommand(id);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadAnnouncementImageCommand(id, file);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAnnouncement(
        Guid id,
        [FromBody] UpdateAnnouncementRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAnnouncementCommand(id, request.BookId, request.Title, request.Content);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAnnouncement(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAnnouncementCommand(id);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }
}
