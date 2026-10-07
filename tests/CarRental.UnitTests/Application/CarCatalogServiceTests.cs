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
    public async Task DeleteCarAsync_ShouldMarkCarAsDeletedAndSave()
    {
        Car car = CreateCar();
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>())).ReturnsAsync(car);

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
    public async Task DeleteCarAsync_ShouldThrowInvalidOperation_WhenCarIsAlreadyDeleted()
    {
        Car car = CreateCar();
        car.MarkAsDeleted(Now.AddDays(-1));
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>())).ReturnsAsync(car);

        Func<Task> act = () => _service.DeleteCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        car.DeletedAt.Should().Be(Now.AddDays(-1));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldClearDeletedAtAndReturnCar()
    {
        Car car = CreateCar();
        car.MarkAsDeleted(Now);
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>())).ReturnsAsync(car);

        CarDto result = await _service.RestoreCarAsync(car.Id);

        car.IsDeleted.Should().BeFalse();
        result.Id.Should().Be(car.Id);
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
        _carRepo.Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>())).ReturnsAsync(car);

        Func<Task> act = () => _service.RestoreCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCarByIdAsync_ShouldReturnNull_WhenRepositoryHidesDeletedCar()
    {
        _carRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Car?)null);

        CarDto? result = await _service.GetCarByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    private static Car CreateCar()
    {
        return new Car(Guid.NewGuid(), ValidVin, "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
    }
}
