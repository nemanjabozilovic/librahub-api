namespace LibraHub.Library.Api.Dtos.Reading;

public class UpdateProgressRequestDto
{
    public string? Format { get; init; }
    public int? Version { get; init; }
    public decimal Percentage { get; init; }
    public int? LastPage { get; init; }
}
