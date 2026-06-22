using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Editions.Queries.GetBookEditions;
using LibraHub.Content.Domain.Books;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Editions.Queries.GetBookEditions;

public class GetBookEditionsHandlerTests
{
    private readonly Mock<IBookEditionRepository> _editionRepository = new();

    private GetBookEditionsHandler CreateHandler() => new(_editionRepository.Object);

    [Fact]
    public async Task Handle_NoEditions_ReturnsEmptyList()
    {
        var bookId = Guid.NewGuid();
        _editionRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BookEdition>());

        var result = await CreateHandler().Handle(new GetBookEditionsQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_FiltersBlockedAndOrders()
    {
        var bookId = Guid.NewGuid();
        var pdfV1 = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 1, Guid.NewGuid());
        var pdfV2 = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 2, Guid.NewGuid());
        var epubV1 = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Epub, 1, Guid.NewGuid());
        var blocked = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 3, Guid.NewGuid());
        blocked.Block();

        _editionRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookEdition> { pdfV1, epubV1, pdfV2, blocked });

        var result = await CreateHandler().Handle(new GetBookEditionsQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        // Epub (1) > Pdf (0) descending by format, then version descending
        Assert.Equal("EPUB", result.Value[0].Format);
        Assert.Equal("PDF", result.Value[1].Format);
        Assert.Equal(2, result.Value[1].Version);
        Assert.Equal(1, result.Value[2].Version);
    }
}
