namespace LibraHub.Library.Domain.Books;

public class BookSnapshot
{
    public Guid BookId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Authors { get; private set; } = string.Empty;
    public string? CoverRef { get; private set; }
    public BookAvailability Availability { get; private set; }
    public string? PriceLabel { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected BookSnapshot()
    {
    } // For EF Core

    public BookSnapshot(
        Guid bookId,
        string title,
        string authors,
        string? coverRef = null,
        string? priceLabel = null)
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("BookId cannot be empty", nameof(bookId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(authors))
            throw new ArgumentException("Authors cannot be empty", nameof(authors));

        BookId = bookId;
        Title = title;
        Authors = authors;
        CoverRef = coverRef;
        Availability = BookAvailability.Available;
        PriceLabel = priceLabel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        string authors,
        string? coverRef = null,
        string? priceLabel = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(authors))
            throw new ArgumentException("Authors cannot be empty", nameof(authors));

        Title = title;
        Authors = authors;
        CoverRef = coverRef;
        PriceLabel = priceLabel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRemoved()
    {
        Availability = BookAvailability.Removed;
        UpdatedAt = DateTime.UtcNow;
    }
}
