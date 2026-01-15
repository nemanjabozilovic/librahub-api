namespace LibraHub.Catalog.Domain.Books;

public class Isbn
{
    public string Value { get; private set; } = string.Empty;

    protected Isbn()
    { }

    public Isbn(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ISBN cannot be empty", nameof(value));
        }

        if (value.Length < 10 || value.Length > 17)
        {
            throw new ArgumentException("ISBN must be between 10 and 17 characters", nameof(value));
        }

        Value = value;
    }

    public static implicit operator string(Isbn isbn) => isbn.Value;

    public static implicit operator Isbn(string value) => new(value);
}
