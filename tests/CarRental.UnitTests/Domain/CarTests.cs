using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CarRental.UnitTests.Domain;

public class CarTests
{
    private const string ValidVin = "1HGCM82633A004352";

    [Fact]
    public void Car_ShouldHaveAvailableStatus_WhenCreated()
    {
        Car car = CreateCar();

        car.Status.Should().Be(CarStatus.Available);
    }

    [Fact]
    public void Car_ShouldSetAllProperties_WhenCreatedWithValidData()
    {
        var id = Guid.NewGuid();
        var car = new Car(id, ValidVin, "BMW", "X5", 2023, CarCategory.Premium, 150m, 5_000);

        car.Id.Should().Be(id);
        car.Vin.Value.Should().Be(ValidVin);
        car.Make.Should().Be("BMW");
        car.Model.Should().Be("X5");
        car.Year.Should().Be(2023);
        car.Category.Should().Be(CarCategory.Premium);
        car.DailyRate.Should().Be(150m);
        car.Mileage.Should().Be(5_000);
        car.Status.Should().Be(CarStatus.Available);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("INVALID")]
    [InlineData("1HGCM82633A00435")]
    [InlineData("1HGCM82633A004352X")]
    [InlineData("1HGCM82633I004352")]
    [InlineData("1HGCM82633O004352")]
    [InlineData("1HGCM82633Q004352")]
    public void Car_ShouldThrow_WhenVinIsInvalid(string invalidVin)
    {
        Action act = () => CreateCar(invalidVin);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Car_ShouldBeRented_WhenStatusIsAvailable()
    {
        Car car = CreateCar();

        car.Rent();

        car.Status.Should().Be(CarStatus.Rented);
    }

    [Fact]
    public void Car_ShouldBeAvailable_WhenCompleteCalledAfterRent()
    {
        Car car = CreateCar();
        car.Rent();

        car.Complete();

        car.Status.Should().Be(CarStatus.Available);
    }

    [Fact]
    public void Car_ShouldTransitionToMaintenance_WhenSentFromAvailable()
    {
        Car car = CreateCar();

        car.SendToMaintenance();

        car.Status.Should().Be(CarStatus.UnderMaintenance);
    }

    [Fact]
    public void Car_ShouldReturnToAvailable_WhenReturnedFromMaintenance()
    {
        Car car = CreateCar();
        car.SendToMaintenance();

        car.ReturnFromMaintenance();

        car.Status.Should().Be(CarStatus.Available);
    }

    [Fact]
    public void Car_ShouldThrow_WhenRentCalledOnRentedCar()
    {
        Car car = CreateCar();
        car.Rent();

        Action act = () => car.Rent();

        act.Should().Throw<CarNotAvailableException>();
    }

    [Fact]
    public void Car_ShouldThrow_WhenCompleteCalledOnAvailableCar()
    {
        Car car = CreateCar();

        Action act = () => car.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Car_ShouldThrow_WhenSendToMaintenanceCalledOnRentedCar()
    {
        Car car = CreateCar();
        car.Rent();

        Action act = () => car.SendToMaintenance();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Car_ShouldThrow_WhenReturnFromMaintenanceCalledOnAvailableCar()
    {
        Car car = CreateCar();

        Action act = () => car.ReturnFromMaintenance();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Car_ShouldNotBeDeleted_WhenCreated()
    {
        Car car = CreateCar();

        car.IsDeleted.Should().BeFalse();
        car.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Car_ShouldBeMarkedDeleted_WhenSoftDeleted()
    {
        Car car = CreateCar();
        var deletedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        car.SoftDelete(deletedAt);

        car.IsDeleted.Should().BeTrue();
        car.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void Car_ShouldThrow_WhenSoftDeletingAlreadyDeletedCar()
    {
        Car car = CreateCar();
        car.SoftDelete(DateTime.UtcNow);

        Action act = () => car.SoftDelete(DateTime.UtcNow);

        act.Should().Throw<CarAlreadyDeletedException>();
    }

    [Fact]
    public void Car_ShouldClearDeletedAt_WhenRestored()
    {
        Car car = CreateCar();
        car.SoftDelete(DateTime.UtcNow);

        car.Restore();

        car.IsDeleted.Should().BeFalse();
        car.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Car_ShouldThrow_WhenRestoringNotDeletedCar()
    {
        Car car = CreateCar();

        Action act = () => car.Restore();

        act.Should().Throw<CarNotDeletedException>();
    }

    [Fact]
    public void Car_ShouldThrow_WhenRentCalledOnDeletedCar()
    {
        Car car = CreateCar();
        car.SoftDelete(DateTime.UtcNow);

        Action act = () => car.Rent();

        act.Should().Throw<CarNotAvailableException>();
    }

    private static Car CreateCar(string? vin = null, CarCategory category = CarCategory.Economy)
    {
        return new Car(Guid.NewGuid(), vin ?? ValidVin, "Toyota", "Camry", 2020, category, 50m, 10_000);
    }
}