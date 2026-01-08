using LibraHub.BuildingBlocks.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LibraHub.Catalog.Application.Announcements.Commands.UploadAnnouncementImage;

public record UploadAnnouncementImageCommand(
    Guid AnnouncementId,
    IFormFile File) : IRequest<Result<string>>;

