using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.DTOs;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace CarRental.UnitTests.Application;

public class CarCatalogServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICarRepository> _carRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();
    private readonly CarCatalogService _service;

    public CarCatalogServiceTests()
    {
        _dateTime.Setup(d => d.UtcNow).Returns(Now);
        _service = new CarCatalogService(_carRepo.Object, _uow.Object, _dateTime.Object);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldSoftDeleteAndSave_WhenCarExists()
    {
        Car car = CreateCar();
        SetupCar(car);

        await _service.DeleteCarAsync(car.Id);

        car.IsDeleted.Should().BeTrue();
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
    public async Task DeleteCarAsync_ShouldThrowCarAlreadyDeleted_WhenCarIsAlreadyDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now.AddDays(-1));
        SetupCar(car);

        Func<Task> act = () => _service.DeleteCarAsync(car.Id);

        await act.Should().ThrowAsync<CarAlreadyDeletedException>();
        car.DeletedAt.Should().Be(Now.AddDays(-1));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldRestoreAndSave_WhenCarIsDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now);
        SetupCar(car);

        CarDto result = await _service.RestoreCarAsync(car.Id);

        car.IsDeleted.Should().BeFalse();
        result.Id.Should().Be(car.Id);
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowCarNotDeleted_WhenCarIsNotDeleted()
    {
        Car car = CreateCar();
        SetupCar(car);

        Func<Task> act = () => _service.RestoreCarAsync(car.Id);

        await act.Should().ThrowAsync<CarNotDeletedException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        Func<Task> act = () => _service.RestoreCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldPassCancellationTokenToDownstreamCalls()
    {
        Car car = CreateCar();
        SetupCar(car);
        using var cts = new CancellationTokenSource();

        await _service.DeleteCarAsync(car.Id, cts.Token);

        _carRepo.Verify(r => r.GetByIdIncludingDeletedAsync(car.Id, cts.Token), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(cts.Token), Times.Once);
    }

    private static Car CreateCar()
    {
        return new Car(Guid.NewGuid(), "1HGCM82633A004352", "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
    }

    private void SetupCar(Car car)
    {
        _carRepo
            .Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);
    }
}
