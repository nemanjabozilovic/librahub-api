namespace LibraHub.Catalog.Api.Dtos.Books;

public record RemoveBookRequestDto
{
    public string Reason { get; init; } = string.Empty;
}
