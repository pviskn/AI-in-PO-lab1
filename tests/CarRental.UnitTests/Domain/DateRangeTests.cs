using CarRental.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CarRental.UnitTests.Domain;

public class DateRangeTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenEndDateIsAfterStartDate()
    {
        DateOnly start = D(2026, 5, 1);
        DateOnly end = D(2026, 5, 8);
        var range = DateRange.Create(start, end);

        range.StartDate.Should().Be(D(2026, 5, 1));
        range.EndDate.Should().Be(D(2026, 5, 8));
    }

    [Fact]
    public void Create_ShouldSucceed_WhenEndDateEqualsStartDate()
    {
        var range = DateRange.Create(D(2026, 5, 1), D(2026, 5, 1));
        range.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldThrow_WhenEndDateIsBeforeStartDate()
    {
        Action act = () => DateRange.Create(D(2026, 5, 10), D(2026, 5, 5));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DurationInDays_ShouldReturnCorrectValue()
    {
        var range = DateRange.Create(D(2026, 5, 1), D(2026, 5, 8));

        range.DurationInDays.Should().Be(8);
    }

    [Theory]
    [InlineData("2026-05-01", "2026-05-10", "2026-05-05", "2026-05-15", true)]
    [InlineData("2026-05-01", "2026-05-10", "2026-05-10", "2026-05-20", true)]
    [InlineData("2026-05-01", "2026-05-10", "2026-05-11", "2026-05-20", false)]
    [InlineData("2026-05-05", "2026-05-15", "2026-05-01", "2026-05-08", true)]
    [InlineData("2026-05-01", "2026-05-20", "2026-05-05", "2026-05-10", true)]
    public void Overlaps_ShouldReturnCorrectResult(
        string s1,
        string e1,
        string s2,
        string e2,
        bool expected)
    {
        var r1 = DateRange.Create(DateOnly.Parse(s1), DateOnly.Parse(e1));
        var r2 = DateRange.Create(DateOnly.Parse(s2), DateOnly.Parse(e2));

        r1.Overlaps(r2).Should().Be(expected);
    }

    [Fact]
    public void Equals_ShouldBeTrue_ForSameDates()
    {
        var r1 = DateRange.Create(D(2026, 5, 1), D(2026, 5, 8));
        var r2 = DateRange.Create(D(2026, 5, 1), D(2026, 5, 8));

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equals_ShouldBeFalse_ForDifferentDates()
    {
        var r1 = DateRange.Create(D(2026, 5, 1), D(2026, 5, 8));
        var r2 = DateRange.Create(D(2026, 5, 1), D(2026, 5, 9));

        r1.Should().NotBe(r2);
    }

    private static DateOnly D(int year, int month, int day) => new(year, month, day);
}
