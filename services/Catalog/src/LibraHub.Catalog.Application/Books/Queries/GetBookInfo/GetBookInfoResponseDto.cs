namespace LibraHub.Catalog.Application.Books.Queries.GetBookInfo;

public record GetBookInfoResponseDto
{
    public Guid BookId { get; init; }
    public bool IsFree { get; init; }
    public bool IsBlocked { get; init; }
}
