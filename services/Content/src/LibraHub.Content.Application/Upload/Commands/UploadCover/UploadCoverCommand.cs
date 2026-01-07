using LibraHub.BuildingBlocks.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LibraHub.Content.Application.Upload.Commands.UploadCover;

public record UploadCoverCommand(
    Guid BookId,
    IFormFile File) : IRequest<Result<Guid>>;
