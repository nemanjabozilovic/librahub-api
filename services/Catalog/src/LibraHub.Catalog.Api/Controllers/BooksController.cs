using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Api.Dtos.Books;
using LibraHub.Catalog.Application.Books.Commands.CreateBook;
using LibraHub.Catalog.Application.Books.Commands.PublishBook;
using LibraHub.Catalog.Application.Books.Commands.RemoveBook;
using LibraHub.Catalog.Application.Books.Commands.SetPricing;
using LibraHub.Catalog.Application.Books.Commands.UnlistBook;
using LibraHub.Catalog.Application.Books.Commands.UpdateBook;
using LibraHub.Catalog.Application.Books.Queries.GetBook;
using LibraHub.Catalog.Application.Books.Queries.SearchBooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Catalog.Api.Controllers;

[ApiController]
[Route("books")]
public class BooksController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SearchBooksResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchBooksQuery(searchTerm, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetBookResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBook(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBookQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBook(
        [FromBody] CreateBookRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookCommand(request.Title);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetBook), new { id = result.Value });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBook(
        Guid id,
        [FromBody] UpdateBookRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBookCommand(
            id,
            request.Description,
            request.Language,
            request.Publisher,
            request.PublicationDate,
            request.Isbn,
            request.Authors,
            request.Categories,
            request.Tags);

        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPost("{id}/pricing")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPricing(
        Guid id,
        [FromBody] SetPricingRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new SetPricingCommand(
            id,
            request.Price,
            request.Currency,
            request.VatRate,
            request.PromoPrice,
            request.PromoStartDate,
            request.PromoEndDate);

        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPost("{id}/publish")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishBook(Guid id, CancellationToken cancellationToken)
    {
        var command = new PublishBookCommand(id);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPost("{id}/unlist")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlistBook(Guid id, CancellationToken cancellationToken)
    {
        var command = new UnlistBookCommand(id);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPost("{id}/remove")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveBook(
        Guid id,
        [FromBody] RemoveBookRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RemoveBookCommand(id, request.Reason);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }
}
