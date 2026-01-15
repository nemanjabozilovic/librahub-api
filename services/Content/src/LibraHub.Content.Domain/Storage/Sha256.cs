namespace LibraHub.Content.Domain.Storage;

public class Sha256
{
    public string Value { get; private set; } = string.Empty;

    private Sha256()
    { }

    public Sha256(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SHA-256 hash cannot be empty", nameof(value));
        }

        if (value.Length != 64)
        {
            throw new ArgumentException("SHA-256 hash must be 64 characters (hex)", nameof(value));
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[0-9a-fA-F]{64}$"))
        {
            throw new ArgumentException("SHA-256 hash must be valid hexadecimal", nameof(value));
        }

        Value = value.ToLowerInvariant();
    }

    public static implicit operator string(Sha256 sha256) => sha256.Value;

    public static implicit operator Sha256(string value) => new(value);

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        if (obj is Sha256 other)
        {
            return Value == other.Value;
        }
        return false;
    }

    public override int GetHashCode() => Value.GetHashCode();
}
