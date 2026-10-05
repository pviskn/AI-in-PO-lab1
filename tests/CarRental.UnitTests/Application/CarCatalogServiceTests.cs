using CarRental.Application.Abstractions.Repositories;
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
    private readonly Mock<ICarRepository> _carRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly CarCatalogService _service;

    public CarCatalogServiceTests()
    {
        _service = new CarCatalogService(_carRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldSoftDeleteAndSave_WhenCarExists()
    {
        Car car = CreateCar();
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        await _service.DeleteCarAsync(car.Id);

        car.IsDeleted.Should().BeTrue();
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Car?)null);

        Func<Task> act = () => _service.DeleteCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldThrowInvalidOperation_WhenCarAlreadyDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete();
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        Func<Task> act = () => _service.DeleteCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldRestoreAndSave_WhenCarIsDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete();
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        CarDto result = await _service.RestoreCarAsync(car.Id);

        result.Id.Should().Be(car.Id);
        car.IsDeleted.Should().BeFalse();
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowInvalidOperation_WhenCarIsNotDeleted()
    {
        Car car = CreateCar();
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        Func<Task> act = () => _service.RestoreCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Car?)null);

        Func<Task> act = () => _service.RestoreCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static Car CreateCar()
    {
        return new Car(Guid.NewGuid(), "1HGCM82633A004352", "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
    }
}
