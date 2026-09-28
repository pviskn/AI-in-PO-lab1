namespace CarRental.Application.Abstractions.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly UtcToday { get; }
}
