using LibraHub.Catalog.Domain.Projections;

namespace LibraHub.Catalog.Domain.Books;

public class Book
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Language { get; private set; }
    public string? Publisher { get; private set; }
    public DateTime? PublicationDate { get; private set; }
    public Isbn? Isbn { get; private set; }
    public BookStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? RemovedBy { get; private set; }
    public string? RemovalReason { get; private set; }
    public DateTime? RemovedAt { get; private set; }

    private readonly List<BookAuthor> _authors = new();
    public virtual IReadOnlyCollection<BookAuthor> Authors => _authors.AsReadOnly();

    private readonly List<BookCategory> _categories = new();
    public virtual IReadOnlyCollection<BookCategory> Categories => _categories.AsReadOnly();

    private readonly List<BookTag> _tags = new();
    public virtual IReadOnlyCollection<BookTag> Tags => _tags.AsReadOnly();

    protected Book()
    { }

    public Book(Guid id, string title)
    {
        Id = id;
        Title = title;
        Status = BookStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(
        string? title = null,
        string? description = null,
        string? language = null,
        string? publisher = null,
        DateTime? publicationDate = null,
        Isbn? isbn = null)
    {
        if (Status == BookStatus.Removed)
        {
            throw new InvalidOperationException("Cannot update removed book");
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            Description = description;
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            Language = language;
        }

        if (!string.IsNullOrWhiteSpace(publisher))
        {
            Publisher = publisher;
        }

        if (publicationDate.HasValue)
        {
            PublicationDate = publicationDate;
        }

        if (isbn != null)
        {
            Isbn = isbn;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAuthor(string authorName)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            throw new ArgumentException("Author name cannot be empty", nameof(authorName));
        }

        if (_authors.Any(a => a.Name == authorName))
        {
            return;
        }

        _authors.Add(new BookAuthor(Id, authorName));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAuthor(string authorName)
    {
        var author = _authors.FirstOrDefault(a => a.Name == authorName);
        if (author != null)
        {
            _authors.Remove(author);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddCategory(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category name cannot be empty", nameof(categoryName));
        }

        if (_categories.Any(c => c.Name == categoryName))
        {
            return;
        }

        _categories.Add(new BookCategory(Id, categoryName));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCategory(string categoryName)
    {
        var category = _categories.FirstOrDefault(c => c.Name == categoryName);
        if (category != null)
        {
            _categories.Remove(category);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new ArgumentException("Tag name cannot be empty", nameof(tagName));
        }

        if (_tags.Any(t => t.Name == tagName))
        {
            return;
        }

        _tags.Add(new BookTag(Id, tagName));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTag(string tagName)
    {
        var tag = _tags.FirstOrDefault(t => t.Name == tagName);
        if (tag != null)
        {
            _tags.Remove(tag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Publish(PricingPolicy pricingPolicy, BookContentState? contentState)
    {
        if (Status == BookStatus.Removed)
        {
            throw new InvalidOperationException("Cannot publish removed book");
        }

        if (Status == BookStatus.Published)
        {
            return;
        }

        ValidatePublishingRequirements(pricingPolicy, contentState);

        Status = BookStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unlist()
    {
        if (Status == BookStatus.Removed)
        {
            throw new InvalidOperationException("Cannot unlist removed book");
        }

        if (Status != BookStatus.Published)
        {
            throw new InvalidOperationException("Only published books can be unlisted");
        }

        Status = BookStatus.Unlisted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Relist()
    {
        if (Status == BookStatus.Removed)
        {
            throw new InvalidOperationException("Cannot relist removed book");
        }

        if (Status != BookStatus.Unlisted)
        {
            throw new InvalidOperationException("Only unlisted books can be relisted");
        }

        Status = BookStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Remove(Guid removedBy, string reason)
    {
        if (Status == BookStatus.Removed)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Removal reason is required", nameof(reason));
        }

        Status = BookStatus.Removed;
        RemovedBy = removedBy;
        RemovalReason = reason;
        RemovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ValidatePublishingRequirements(PricingPolicy pricingPolicy, BookContentState? contentState)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("Title is required for publishing");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new InvalidOperationException("Description is required for publishing");
        }

        if (string.IsNullOrWhiteSpace(Language))
        {
            throw new InvalidOperationException("Language is required for publishing");
        }

        if (!pricingPolicy.IsValid())
        {
            throw new InvalidOperationException("Pricing policy must be valid");
        }

        if (contentState == null || !contentState.IsReadyForPublishing())
        {
            throw new InvalidOperationException("Book content is not ready for publishing (missing cover or editions)");
        }
    }

    public bool CanBePublished(PricingPolicy pricingPolicy, BookContentState? contentState)
    {
        try
        {
            ValidatePublishingRequirements(pricingPolicy, contentState);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class BookAuthor
{
    public Guid BookId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    protected BookAuthor()
    { }

    public BookAuthor(Guid bookId, string name)
    {
        BookId = bookId;
        Name = name;
    }
}

public class BookCategory
{
    public Guid BookId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    protected BookCategory()
    { }

    public BookCategory(Guid bookId, string name)
    {
        BookId = bookId;
        Name = name;
    }
}

public class BookTag
{
    public Guid BookId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    protected BookTag()
    { }

    public BookTag(Guid bookId, string name)
    {
        BookId = bookId;
        Name = name;
    }
}
