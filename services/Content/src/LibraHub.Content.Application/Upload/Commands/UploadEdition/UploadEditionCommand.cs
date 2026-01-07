using LibraHub.BuildingBlocks.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LibraHub.Content.Application.Upload.Commands.UploadEdition;

public record UploadEditionCommand(
    Guid BookId,
    IFormFile File,
    string Format) : IRequest<Result<Guid>>;
