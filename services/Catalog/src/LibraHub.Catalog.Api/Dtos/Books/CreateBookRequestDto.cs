namespace LibraHub.Catalog.Api.Dtos.Books;

public record CreateBookRequestDto
{
    public string Title { get; init; } = string.Empty;
}
