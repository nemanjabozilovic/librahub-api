using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Commands.CreateBook;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Commands.CreateBook;

public class CreateBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();

    private CreateBookHandler CreateHandler() => new(_bookRepository.Object, _outboxWriter.Object);

    private static CreateBookCommand CreateCommand(string isbn = "1234567890") => new(
        Title: "Clean Code",
        Description: "A handbook",
        Language: "en",
        Publisher: "Prentice",
        PublicationDate: new DateTimeOffset(2008, 8, 1, 0, 0, 0, TimeSpan.Zero),
        Isbn: isbn,
        Authors: new List<string> { "Robert Martin" },
        Categories: new List<string> { "Programming" },
        Tags: new List<string> { "clean" });

    [Fact]
    public async Task Handle_ValidCommand_PersistsBookAndWritesEvent()
    {
        Book? captured = null;
        _bookRepository
            .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((b, _) => captured = b)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("Clean Code", captured!.Title);
        Assert.Equal(BookStatus.Draft, captured.Status);
        Assert.Single(captured.Authors);
        Assert.Single(captured.Categories);
        Assert.Single(captured.Tags);
        Assert.Equal("1234567890", captured.Isbn!.Value);

        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<BookCreatedV1>(e => e.BookId == result.Value && e.Title == "Clean Code"),
            Contracts.Common.EventTypes.BookCreated,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidIsbn_ReturnsValidationErrorAndDoesNotPersist()
    {
        var result = await CreateHandler().Handle(CreateCommand(isbn: "short"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _bookRepository.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<BookCreatedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullTags_DoesNotThrow()
    {
        var command = CreateCommand() with { Tags = null };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
