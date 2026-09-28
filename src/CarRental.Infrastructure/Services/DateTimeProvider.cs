using CarRental.Application.Abstractions.Services;

namespace CarRental.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
}