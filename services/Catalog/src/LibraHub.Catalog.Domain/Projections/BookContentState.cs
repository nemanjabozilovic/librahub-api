namespace LibraHub.Catalog.Domain.Projections;

public class BookContentState
{
    public Guid BookId { get; private set; }
    public bool HasCover { get; private set; }
    public string? CoverRef { get; private set; }
    public bool HasEdition { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private BookContentState()
    { } // For EF Core

    public BookContentState(Guid bookId)
    {
        BookId = bookId;
        HasCover = false;
        HasEdition = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCover(string coverRef)
    {
        HasCover = true;
        CoverRef = coverRef;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEdition()
    {
        HasEdition = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsReadyForPublishing()
    {
        return HasCover && HasEdition;
    }
}
