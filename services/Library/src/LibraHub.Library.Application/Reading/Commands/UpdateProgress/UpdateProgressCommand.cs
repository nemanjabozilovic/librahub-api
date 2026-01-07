using MediatR;

namespace LibraHub.Library.Application.Reading.Commands.UpdateProgress;

public class UpdateProgressCommand : IRequest<LibraHub.BuildingBlocks.Results.Result>
{
    public Guid BookId { get; init; }
    public string? Format { get; init; }
    public int? Version { get; init; }
    public decimal Percentage { get; init; }
    public int? LastPage { get; init; }
}
