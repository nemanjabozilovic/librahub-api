namespace LibraHub.Content.Application.Editions.Queries.GetBookEditions;

public static class BookEditionDtoMapper
{
    public static BookEditionDto MapFromBookEdition(Domain.Books.BookEdition edition)
    {
        return new BookEditionDto
        {
            Id = edition.Id,
            Format = edition.Format.ToString().ToUpperInvariant(),
            Version = edition.Version,
            UploadedAt = new DateTimeOffset(edition.UploadedAt, TimeSpan.Zero)
        };
    }
}
