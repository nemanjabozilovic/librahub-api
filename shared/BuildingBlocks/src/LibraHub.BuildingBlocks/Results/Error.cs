namespace LibraHub.BuildingBlocks.Results;

public class Error
{
    public string Code { get; }
    public string Message { get; }
    public Dictionary<string, object>? Metadata { get; }

    public Error(string code, string message, Dictionary<string, object>? metadata = null)
    {
        Code = code;
        Message = message;
        Metadata = metadata;
    }

    public static Error NotFound(string resource) => new("NOT_FOUND", $"{resource} not found");

    public static Error Validation(string message) => new("VALIDATION_ERROR", message);

    public static Error Conflict(string message) => new("CONFLICT", message);

    public static Error Unauthorized(string message = "Unauthorized") => new("UNAUTHORIZED", message);

    public static Error Forbidden(string message = "Forbidden") => new("FORBIDDEN", message);

    public static Error Unexpected(string message = "Unexpected error") => new("UNEXPECTED", message);
}
