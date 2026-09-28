namespace CarRental.Domain.ValueObjects;

public sealed class DateRange : IEquatable<DateRange>
{
    public DateOnly StartDate { get; }

    public DateOnly EndDate { get; }

    public int DurationInDays => EndDate.DayNumber - StartDate.DayNumber + 1;

    private DateRange(DateOnly startDate, DateOnly endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static DateRange Create(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("дата начала не может быть позже даты окончания");

        return new DateRange(startDate, endDate);
    }

    public bool Overlaps(DateRange other)
    {
        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }

    public bool Equals(DateRange? other)
    {
        if (other is null)
            return false;

        return StartDate == other.StartDate && EndDate == other.EndDate;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DateRange);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartDate, EndDate);
    }
}
