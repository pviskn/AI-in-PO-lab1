using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.DTOs;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CarRental.UnitTests.Application;

public class CarCatalogServiceTests
{
    private const string ValidVin = "1HGCM82633A004352";

    private static readonly DateTime Now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

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
    public async Task DeleteCarAsync_ShouldSetDeletedAtAndSave_WhenCarIsActive()
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
    public async Task RestoreCarAsync_ShouldClearDeletedAtAndSave_WhenCarIsDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now);
        SetupCar(car);

        await _service.RestoreCarAsync(car.Id);

        car.DeletedAt.Should().BeNull();
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        Func<Task> act = () => _service.RestoreCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
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
    public async Task GetCarByIdAsync_ShouldReturnNull_WhenRepositoryHidesDeletedCar()
    {
        Car car = CreateCar();
        car.SoftDelete(Now);
        SetupCar(car);

        CarDto? result = await _service.GetCarByIdAsync(car.Id);

        result.Should().BeNull();
        _carRepo.Verify(r => r.GetByIdAsync(car.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Car CreateCar()
    {
        return new Car(Guid.NewGuid(), ValidVin, "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
    }

    private void SetupCar(Car car)
    {
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);
        _carRepo.Setup(r => r.GetByIdAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car.IsDeleted ? null : car);
    }
}
