using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.Common;
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
    public async Task SoftDeleteCarAsync_ShouldMarkCarDeletedAndSave_WhenCarIsActive()
    {
        Car car = CreateCar();
        SetupCarIncludingDeleted(car);

        await _service.SoftDeleteCarAsync(car.Id);

        car.DeletedAt.Should().Be(Now);
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCarAsync_ShouldThrowInvalidOperation_WhenCarIsAlreadyDeleted()
    {
        Car car = CreateCar();
        DateTime originalDeletedAt = Now.AddDays(-1);
        car.SoftDelete(originalDeletedAt);
        SetupCarIncludingDeleted(car);

        Func<Task> act = () => _service.SoftDeleteCarAsync(car.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        car.DeletedAt.Should().Be(originalDeletedAt);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteCarAsync_ShouldThrowKeyNotFound_WhenCarDoesNotExist()
    {
        Func<Task> act = () => _service.SoftDeleteCarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldClearDeletionAndSave_WhenCarIsDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now);
        SetupCarIncludingDeleted(car);

        await _service.RestoreCarAsync(car.Id);

        car.IsDeleted.Should().BeFalse();
        _carRepo.Verify(r => r.Update(car), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreCarAsync_ShouldThrowInvalidOperation_WhenCarIsNotDeleted()
    {
        Car car = CreateCar();
        SetupCarIncludingDeleted(car);

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

    [Fact]
    public async Task GetCarByIdAsync_ShouldReturnNull_WhenRepositoryHidesDeletedCar()
    {
        Car car = CreateCar();
        car.SoftDelete(Now);
        SetupCarIncludingDeleted(car);

        CarDto? result = await _service.GetCarByIdAsync(car.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCarByIdAsync_ShouldReturnDto_WhenCarIsActive()
    {
        Car car = CreateCar();
        _carRepo.Setup(r => r.GetByIdAsync(car.Id, It.IsAny<CancellationToken>())).ReturnsAsync(car);

        CarDto? result = await _service.GetCarByIdAsync(car.Id);

        result.Should().NotBeNull().And.BeOfType<CarDto>().Which.Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task ChangeCarStatusAsync_ShouldThrowKeyNotFound_WhenCarIsDeleted()
    {
        Car car = CreateCar();
        car.SoftDelete(Now);
        SetupCarIncludingDeleted(car);

        Func<Task> act = () => _service.ChangeCarStatusAsync(car.Id, CarStatus.UnderMaintenance);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        car.Status.Should().Be(CarStatus.Available);
    }

    [Fact]
    public async Task GetCarsAsync_ShouldReturnRepositoryPage()
    {
        Car car = CreateCar();
        _carRepo
            .Setup(r => r.GetPagedAsync(null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Car> { car }, 1));

        PagedResult<CarDto> result = await _service.GetCarsAsync(new CarFilterDto(null, null, null, null, 1, 20));

        result.Items.Should().ContainSingle().Which.Id.Should().Be(car.Id);
        result.TotalCount.Should().Be(1);
    }

    private static Car CreateCar()
    {
        return new Car(Guid.NewGuid(), "1HGCM82633A004352", "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
    }

    private void SetupCarIncludingDeleted(Car car)
    {
        _carRepo
            .Setup(r => r.GetByIdIncludingDeletedAsync(car.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);
    }
}
