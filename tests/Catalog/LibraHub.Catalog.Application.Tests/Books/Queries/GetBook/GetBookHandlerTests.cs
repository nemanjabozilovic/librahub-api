using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Queries.GetBook;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Projections;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Queries.GetBook;

public class GetBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();
    private readonly Mock<IBookContentStateRepository> _contentStateRepository = new();
    private readonly Mock<IContentReadClient> _contentReadClient = new();
    private readonly Mock<ICache> _cache = new();
    private readonly CatalogOptions _options = new() { GatewayBaseUrl = "https://gw", ContentApiUrl = "https://content" };

    private GetBookHandler CreateHandler() => new(
        _bookRepository.Object,
        _pricingRepository.Object,
        _contentStateRepository.Object,
        _contentReadClient.Object,
        Microsoft.Extensions.Options.Options.Create(_options),
        _cache.Object);

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedWithoutRepositoryAccess()
    {
        var bookId = Guid.NewGuid();
        var cached = new GetBookResponseDto { Id = bookId, Title = "Cached" };
        _cache.Setup(c => c.GetAsync<GetBookResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await CreateHandler().Handle(new GetBookQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cached", result.Value.Title);
        _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync<GetBookResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((GetBookResponseDto?)null);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new GetBookQuery(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RemovedBook_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync<GetBookResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((GetBookResponseDto?)null);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Removed(bookId));

        var result = await CreateHandler().Handle(new GetBookQuery(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_Found_MapsResponseCachesAndBuildsCoverUrl()
    {
        var bookId = Guid.NewGuid();
        var book = BookFactory.Published(bookId, "The Book");
        var content = new BookContentState(bookId);
        content.SetCover("c.png");
        content.SetEdition();

        _cache.Setup(c => c.GetAsync<GetBookResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((GetBookResponseDto?)null);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(10m, Currency.USD)));
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(content);
        _contentReadClient.Setup(c => c.GetBookEditionsAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<BookEditionInfoDto>
            {
                new() { Id = Guid.NewGuid(), Format = "pdf", Version = 1, UploadedAt = DateTimeOffset.UtcNow }
            }));

        var result = await CreateHandler().Handle(new GetBookQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("The Book", result.Value.Title);
        Assert.Equal("https://gw/api/covers/c.png", result.Value.CoverUrl);
        Assert.True(result.Value.HasEdition);
        Assert.Single(result.Value.Editions);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<GetBookResponseDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ContentClientFails_FallsBackToEmptyEditions()
    {
        var bookId = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync<GetBookResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((GetBookResponseDto?)null);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((PricingPolicy?)null);
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookContentState?)null);
        _contentReadClient.Setup(c => c.GetBookEditionsAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<BookEditionInfoDto>>(LibraHub.BuildingBlocks.Results.Error.Unexpected("down")));

        var result = await CreateHandler().Handle(new GetBookQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Editions);
        Assert.False(result.Value.HasEdition);
        Assert.Null(result.Value.CoverUrl);
    }
}
