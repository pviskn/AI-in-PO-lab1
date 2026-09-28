using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CarRental.UnitTests.Application;

public class RentalPricingServiceTests
{
    private const string ValidVin = "1HGCM82633A004352";

    private readonly RentalPricingService _service = new();

    [Fact]
    public void CalculateBasePrice_ShouldMultiplyDailyRateByDays()
    {
        Car car = CreateCar(100m);
        var start = new DateOnly(2026, 5, 1);
        var end = new DateOnly(2026, 5, 8);

        decimal result = _service.CalculateBasePrice(car, start, end);

        result.Should().Be(700m);
    }

    [Theory]
    [InlineData(100, 3, 300)]
    [InlineData(200, 5, 1000)]
    public void CalculateBasePrice_ShouldReturnCorrectAmount(decimal dailyRate, int days, decimal expected)
    {
        Car car = CreateCar(dailyRate);
        var start = new DateOnly(2026, 5, 1);
        DateOnly end = start.AddDays(days);

        decimal result = _service.CalculateBasePrice(car, start, end);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateLateFee_ShouldBeZero_WhenReturnedOnTime()
    {
        Car car = CreateCar(100m);
        var planned = new DateOnly(2026, 5, 10);

        decimal fee = _service.CalculateLateFee(car, planned, planned);

        fee.Should().Be(0m);
    }

    [Fact]
    public void CalculateLateFee_ShouldBeZero_WhenReturnedEarly()
    {
        Car car = CreateCar(100m);
        var planned = new DateOnly(2026, 5, 10);
        var actual = new DateOnly(2026, 5, 8);

        decimal fee = _service.CalculateLateFee(car, planned, actual);

        fee.Should().Be(0m);
    }

    [Fact]
    public void CalculateLateFee_ShouldBePositive_WhenReturnedLate()
    {
        Car car = CreateCar(100m);
        var planned = new DateOnly(2026, 5, 10);
        var actual = new DateOnly(2026, 5, 12);

        decimal fee = _service.CalculateLateFee(car, planned, actual);

        fee.Should().Be(300m);
    }

    [Theory]
    [InlineData(100, 1, 150)]
    [InlineData(100, 3, 450)]
    [InlineData(200, 2, 600)]
    public void CalculateLateFee_ShouldApplyMultiplier(decimal dailyRate, int lateDays, decimal expected)
    {
        Car car = CreateCar(dailyRate);
        var planned = new DateOnly(2026, 5, 10);
        DateOnly actual = planned.AddDays(lateDays);

        decimal fee = _service.CalculateLateFee(car, planned, actual);

        fee.Should().Be(expected);
    }

    [Fact]
    public void CalculateTotalPrice_ShouldSumAllComponents()
    {
        decimal total = _service.CalculateTotalPrice(700m, 300m, 200m);

        total.Should().Be(1200m);
    }

    [Fact]
    public void CalculateTotalPrice_ShouldEqualBasePrice_WhenNoFees()
    {
        decimal total = _service.CalculateTotalPrice(500m, 0m, 0m);

        total.Should().Be(500m);
    }

    private static Car CreateCar(decimal dailyRate)
    {
        return new Car(Guid.NewGuid(), ValidVin, "Toyota", "Camry", 2020, CarCategory.Economy, dailyRate, 0);
    }
}
