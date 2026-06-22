using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Commands.UploadAvatar;
using LibraHub.Identity.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Commands;

public class UploadAvatarHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IConfiguration> _configuration = new();

    public UploadAvatarHandlerTests()
    {
        _configuration.Setup(c => c["Storage:AvatarsBucketName"]).Returns("avatars");
    }

    private UploadAvatarHandler CreateHandler() => new(
        _userRepository.Object,
        _objectStorage.Object,
        _configuration.Object,
        Mock.Of<ILogger<UploadAvatarHandler>>());

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    private static IFormFile CreateFile(string fileName = "pic.png", long length = 1024, string contentType = "image/png")
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.FileName).Returns(fileName);
        file.SetupGet(f => f.Length).Returns(length);
        file.SetupGet(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return file.Object;
    }

    [Fact]
    public async Task Handle_NoFile_ReturnsValidationError()
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.Length).Returns(0);

        var result = await CreateHandler().Handle(new UploadAvatarCommand(Guid.NewGuid(), file.Object), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ReturnsValidationError()
    {
        var file = CreateFile(length: 6 * 1024 * 1024);

        var result = await CreateHandler().Handle(new UploadAvatarCommand(Guid.NewGuid(), file), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_DisallowedExtension_ReturnsValidationError()
    {
        var file = CreateFile(fileName: "doc.pdf");

        var result = await CreateHandler().Handle(new UploadAvatarCommand(Guid.NewGuid(), file), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new UploadAvatarCommand(Guid.NewGuid(), CreateFile()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_MissingBucketConfig_ReturnsValidationError()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _configuration.Setup(c => c["Storage:AvatarsBucketName"]).Returns((string?)null);

        var result = await CreateHandler().Handle(new UploadAvatarCommand(user.Id, CreateFile()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UploadThrows_ReturnsValidationError()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _objectStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage down"));

        var result = await CreateHandler().Handle(new UploadAvatarCommand(user.Id, CreateFile()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidFile_UploadsAndUpdatesUser()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new UploadAvatarCommand(user.Id, CreateFile()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith($"/api/users/{user.Id}/avatar/", result.Value);
        Assert.Equal(result.Value, user.Avatar);
        _objectStorage.Verify(s => s.UploadAsync("avatars", It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingAvatar_DeletesOldBeforeUpload()
    {
        var user = CreateUser();
        user.UpdateAvatar($"https://gw.test/api/users/{user.Id}/avatar/old.png");
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new UploadAvatarCommand(user.Id, CreateFile()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _objectStorage.Verify(s => s.DeleteAsync("avatars", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
