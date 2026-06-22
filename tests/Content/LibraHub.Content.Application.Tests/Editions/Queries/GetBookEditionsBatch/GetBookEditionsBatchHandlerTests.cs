using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Editions.Queries.GetBookEditionsBatch;
using LibraHub.Content.Domain.Books;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Editions.Queries.GetBookEditionsBatch;

public class GetBookEditionsBatchHandlerTests
{
    private readonly Mock<IBookEditionRepository> _editionRepository = new();

    private GetBookEditionsBatchHandler CreateHandler() => new(_editionRepository.Object);

    [Fact]
    public async Task Handle_EmptyBookIds_ReturnsEmptyDictionary()
    {
        var result = await CreateHandler().Handle(new GetBookEditionsBatchQuery(new List<Guid>()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        _editionRepository.Verify(r => r.GetByBookIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GroupsByBookIdAndIncludesEmptyForMissing()
    {
        var bookA = Guid.NewGuid();
        var bookB = Guid.NewGuid();
        var bookC = Guid.NewGuid();

        var a1 = new BookEdition(Guid.NewGuid(), bookA, BookFormat.Pdf, 1, Guid.NewGuid());
        var a2 = new BookEdition(Guid.NewGuid(), bookA, BookFormat.Epub, 1, Guid.NewGuid());
        var bBlocked = new BookEdition(Guid.NewGuid(), bookB, BookFormat.Pdf, 1, Guid.NewGuid());
        bBlocked.Block();

        _editionRepository
            .Setup(r => r.GetByBookIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookEdition> { a1, a2, bBlocked });

        var result = await CreateHandler().Handle(new GetBookEditionsBatchQuery(new List<Guid> { bookA, bookB, bookC }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(2, result.Value[bookA].Count);
        // bookB had only a blocked edition -> filtered out, empty list ensured
        Assert.Empty(result.Value[bookB]);
        Assert.Empty(result.Value[bookC]);
        // ordering is by the DTO format string (descending), so "PDF" comes before "EPUB"
        Assert.Equal("PDF", result.Value[bookA][0].Format);
    }
}
