using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CarRental.UnitTests.Application;

public class CarCatalogServiceTests
{
    private static readonly DateTime Now = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICarRepository> _carRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly CarCatalogService _service;

    public CarCatalogServiceTests()
    {
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
        _service = new CarCatalogService(_carRepo.Object, _uow.Object, _dateTimeProvider.Object);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldSoftDeleteAndSave_WhenCarExists()
    {
        Car car = CreateCar();
        SetupCar(car);

        await _service.DeleteCarAsync(car.Id);

        car.DeletedAt.Should().Be(Now);
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        Func<Task> act = () => _service.DeleteCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldThrowInvalidOperation_WhenCarAlreadyDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now.AddDays(-1));
        SetupCar(car);

        Func<Task> act = () => _service.DeleteCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        car.DeletedAt.Should().Be(Now.AddDays(-1));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldRestoreAndSave_WhenCarIsDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now.AddDays(-1));
        SetupCar(car);

        await _service.RestoreCarAsync(car.Id);

        car.IsDeleted.Should().BeFalse();
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowInvalidOperation_WhenCarIsNotDeleted()
    {
        Car car = CreateCar();
        SetupCar(car);

        Func<Task> act = () => _service.RestoreCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        Func<Task> act = () => _service.RestoreCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static Car CreateCar()
    {
        return new Car(Guid.NewGuid(), "1HGCM82633A004352", "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
    }

    private void SetupCar(Car car)
    {
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>())).ReturnsAsync(car);
    }
}
