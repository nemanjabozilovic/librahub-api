namespace LibraHub.Catalog.Api.Dtos.Common;

public record ErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public ErrorResponse(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
