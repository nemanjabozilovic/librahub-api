using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Projections;

namespace LibraHub.Catalog.Application.Tests.TestHelpers;

// Builds a Book aggregate in the requested status via public transitions, since setters are private.
public static class BookFactory
{
    public static Book Draft(Guid id, string title = "Title")
    {
        var book = new Book(id, title);
        book.UpdateMetadata(description: "Desc", language: "en");
        return book;
    }

    public static (PricingPolicy Pricing, BookContentState Content) PublishablePrerequisites(Guid bookId)
    {
        var pricing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(10m, "USD"));
        var content = new BookContentState(bookId);
        content.SetCover("cover.png");
        content.SetEdition();
        return (pricing, content);
    }

    public static Book Published(Guid id, string title = "Title")
    {
        var book = Draft(id, title);
        var (pricing, content) = PublishablePrerequisites(id);
        book.Publish(pricing, content);
        return book;
    }

    public static Book Unlisted(Guid id, string title = "Title")
    {
        var book = Published(id, title);
        book.Unlist();
        return book;
    }

    public static Book Removed(Guid id, string title = "Title")
    {
        var book = Draft(id, title);
        book.Remove(Guid.NewGuid(), "reason");
        return book;
    }
}
