namespace XSenseExtractor;

public interface IExpirable
{
    bool IsExpired { get; }
}

public class Expirable<T> : IExpirable
{
    public Expirable(T value, DateTime expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public Expirable(T value, DateTimeOffset expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public T Value { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsExpired => ExpiresAt < DateTimeOffset.UtcNow;
}