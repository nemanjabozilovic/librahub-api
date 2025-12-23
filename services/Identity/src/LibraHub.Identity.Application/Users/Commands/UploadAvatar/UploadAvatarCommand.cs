using LibraHub.BuildingBlocks.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LibraHub.Identity.Application.Users.Commands.UploadAvatar;

public record UploadAvatarCommand(Guid UserId, IFormFile File) : IRequest<Result<string>>;

