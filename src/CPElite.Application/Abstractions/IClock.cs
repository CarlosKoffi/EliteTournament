namespace CPElite.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
