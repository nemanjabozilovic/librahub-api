using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Queries.SearchBooks;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Projections;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Queries.SearchBooks;

public class SearchBooksHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();
    private readonly Mock<IBookContentStateRepository> _contentStateRepository = new();
    private readonly Mock<IContentReadClient> _contentReadClient = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ICache> _cache = new();
    private readonly CatalogOptions _options = new() { GatewayBaseUrl = "https://gw", ContentApiUrl = "https://content" };

    private SearchBooksHandler CreateHandler() => new(
        _bookRepository.Object,
        _pricingRepository.Object,
        _contentStateRepository.Object,
        _contentReadClient.Object,
        _currentUser.Object,
        Microsoft.Extensions.Options.Options.Create(_options),
        _cache.Object);

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedWithoutRepositoryAccess()
    {
        var cached = new SearchBooksResponseDto { TotalCount = 7 };
        _cache.Setup(c => c.GetAsync<SearchBooksResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await CreateHandler().Handle(new SearchBooksQuery("x"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.TotalCount);
        _bookRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesMapsAndCaches()
    {
        var bookId = Guid.NewGuid();
        var book = BookFactory.Published(bookId, "Found");
        var content = new BookContentState(bookId);
        content.SetCover("c.png");
        content.SetEdition();

        _cache.Setup(c => c.GetAsync<SearchBooksResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((SearchBooksResponseDto?)null);
        _currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        _bookRepository.Setup(r => r.SearchAsync("term", 1, 20, false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Book> { book });
        _bookRepository.Setup(r => r.CountSearchAsync("term", false, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _pricingRepository.Setup(r => r.GetByBookIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PricingPolicy> { [bookId] = new(Guid.NewGuid(), bookId, new Money(10m, Currency.USD)) });
        _contentStateRepository.Setup(r => r.GetByBookIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, BookContentState> { [bookId] = content });
        _contentReadClient.Setup(c => c.GetBookEditionsBatchAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new Dictionary<Guid, List<BookEditionInfoDto>>
            {
                [bookId] = new() { new() { Id = Guid.NewGuid(), Format = "pdf", Version = 1, UploadedAt = DateTimeOffset.UtcNow } }
            }));

        var result = await CreateHandler().Handle(new SearchBooksQuery("term"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Books);
        Assert.Equal("https://gw/api/covers/c.png", result.Value.Books[0].CoverUrl);
        Assert.True(result.Value.Books[0].HasEdition);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SearchBooksResponseDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LibrarianRole_IncludesAllStatuses()
    {
        _cache.Setup(c => c.GetAsync<SearchBooksResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((SearchBooksResponseDto?)null);
        _currentUser.Setup(u => u.IsInRole("Librarian")).Returns(true);
        _bookRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Book>());
        _bookRepository.Setup(r => r.CountSearchAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _pricingRepository.Setup(r => r.GetByBookIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, PricingPolicy>());
        _contentStateRepository.Setup(r => r.GetByBookIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, BookContentState>());
        _contentReadClient.Setup(c => c.GetBookEditionsBatchAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new Dictionary<Guid, List<BookEditionInfoDto>>()));

        var result = await CreateHandler().Handle(new SearchBooksQuery(null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _bookRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
