using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUserAvatar;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Queries;

public class GetUserAvatarQueryHandlerTests
{
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IConfiguration> _configuration = new();

    public GetUserAvatarQueryHandlerTests()
    {
        _configuration.Setup(c => c["Storage:AvatarsBucketName"]).Returns("avatars");
    }

    private GetUserAvatarQueryHandler CreateHandler() => new(
        _objectStorage.Object,
        _configuration.Object);

    [Fact]
    public async Task Handle_EmptyFileName_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetUserAvatarQuery(Guid.NewGuid(), " "), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PathTraversalFileName_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetUserAvatarQuery(Guid.NewGuid(), "../secret.png"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_DisallowedExtension_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetUserAvatarQuery(Guid.NewGuid(), "file.pdf"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_MissingBucketConfig_ReturnsValidationError()
    {
        _configuration.Setup(c => c["Storage:AvatarsBucketName"]).Returns((string?)null);

        var result = await CreateHandler().Handle(new GetUserAvatarQuery(Guid.NewGuid(), "pic.png"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_AvatarNotInStorage_ReturnsNotFound()
    {
        _objectStorage
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(new GetUserAvatarQuery(Guid.NewGuid(), "pic.png"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ExistingAvatar_ReturnsFileWithContentType()
    {
        var userId = Guid.NewGuid();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _objectStorage
            .Setup(s => s.ExistsAsync("avatars", $"users/{userId}/avatar/pic.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _objectStorage
            .Setup(s => s.DownloadAsync("avatars", $"users/{userId}/avatar/pic.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var result = await CreateHandler().Handle(new GetUserAvatarQuery(userId, "pic.png"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Same(stream, result.Value.Content);
    }
}
