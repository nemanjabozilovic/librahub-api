using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Covers.Queries.GetBookCover;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Storage;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Covers.Queries.GetBookCover;

public class GetBookCoverHandlerTests
{
    private readonly Mock<ICoverRepository> _coverRepository = new();
    private readonly Mock<IStoredObjectRepository> _storedObjectRepository = new();

    private GetBookCoverHandler CreateHandler() => new(_coverRepository.Object, _storedObjectRepository.Object);

    [Fact]
    public async Task Handle_NoCover_ReturnsNullRef()
    {
        var bookId = Guid.NewGuid();
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);

        var result = await CreateHandler().Handle(new GetBookCoverQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CoverRef);
    }

    [Fact]
    public async Task Handle_StoredObjectMissing_ReturnsNullRef()
    {
        var bookId = Guid.NewGuid();
        var cover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);
        _storedObjectRepository.Setup(s => s.GetByIdAsync(cover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync((StoredObject?)null);

        var result = await CreateHandler().Handle(new GetBookCoverQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CoverRef);
    }

    [Fact]
    public async Task Handle_AccessibleCover_ReturnsObjectKey()
    {
        var bookId = Guid.NewGuid();
        var cover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);
        var stored = new StoredObject(Guid.NewGuid(), bookId, "books/x/cover/c.png", "image/png", 10, new Sha256(new string('a', 64)));
        _storedObjectRepository.Setup(s => s.GetByIdAsync(cover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateHandler().Handle(new GetBookCoverQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("books/x/cover/c.png", result.Value.CoverRef);
    }

    [Fact]
    public async Task Handle_BlockedCover_ReturnsNullRef()
    {
        var bookId = Guid.NewGuid();
        var cover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        cover.Block();
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);

        var result = await CreateHandler().Handle(new GetBookCoverQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CoverRef);
        _storedObjectRepository.Verify(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
