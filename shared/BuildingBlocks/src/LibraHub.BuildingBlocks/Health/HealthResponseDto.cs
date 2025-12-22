namespace LibraHub.BuildingBlocks.Health;

public record HealthResponseDto
{
    public string Status { get; init; } = string.Empty;
    public List<HealthCheckResultDto> Checks { get; init; } = new();
}

public record HealthCheckResultDto
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
}
