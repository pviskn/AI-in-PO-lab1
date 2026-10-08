using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.DTOs;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CarRental.UnitTests.Application;

public class RentalRequestServiceTests
{
    private readonly Mock<IRentalRequestRepository> _requestRepo = new();
    private readonly Mock<IRentalContractRepository> _contractRepo = new();
    private readonly Mock<ICarRepository> _carRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRentalEligibilityService> _eligibility = new();
    private readonly Mock<IRentalPricingService> _pricing = new();
    private readonly RentalRequestService _service;

    public RentalRequestServiceTests()
    {
        _service = new RentalRequestService(
            _requestRepo.Object,
            _contractRepo.Object,
            _carRepo.Object,
            _userRepo.Object,
            _uow.Object,
            _eligibility.Object,
            _pricing.Object);
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldCancelAndSave_WhenClientIsOwnerAndRequestIsPending()
    {
        RentalRequest request = CreateRequestWithDetails();
        SetupRequest(request);

        RentalRequestDto result = await _service.CancelRequestAsync(request.Id, request.UserId);

        result.Status.Should().Be(RentalRequestStatus.Cancelled);
        request.Status.Should().Be(RentalRequestStatus.Cancelled);
        _requestRepo.Verify(r => r.Update(request), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldThrowInvalidOperation_WhenClientIsNotOwner()
    {
        RentalRequest request = CreateRequestWithDetails();
        SetupRequest(request);

        Func<Task> act = () => _service.CancelRequestAsync(request.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
        request.Status.Should().Be(RentalRequestStatus.Pending);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldThrowInvalidOperation_WhenRequestIsNotPending()
    {
        RentalRequest request = CreateRequestWithDetails();
        request.Reject("отказ");
        SetupRequest(request);

        Func<Task> act = () => _service.CancelRequestAsync(request.Id, request.UserId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        request.Status.Should().Be(RentalRequestStatus.Rejected);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldThrowKeyNotFound_WhenRequestDoesNotExist()
    {
        _requestRepo
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RentalRequest?)null);

        Func<Task> act = () => _service.CancelRequestAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveRequestAsync_ShouldThrowInvalidOperation_WhenRequestIsCancelled()
    {
        RentalRequest request = CreateCancelledRequest();

        Func<Task> act = () => _service.ApproveRequestAsync(request.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        request.Car.Status.Should().Be(CarStatus.Available);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectRequestAsync_ShouldThrowInvalidOperation_WhenRequestIsCancelled()
    {
        RentalRequest request = CreateCancelledRequest();

        Func<Task> act = () => _service.RejectRequestAsync(request.Id, "причина");

        await act.Should().ThrowAsync<InvalidOperationException>();
        request.Status.Should().Be(RentalRequestStatus.Cancelled);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteRentalAsync_ShouldThrowInvalidOperation_WhenRequestIsCancelled()
    {
        RentalRequest request = CreateCancelledRequest();
        var dto = new CompleteRentalDto(request.EndDate, 0m);

        Func<Task> act = () => _service.CompleteRentalAsync(request.Id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
        request.Status.Should().Be(RentalRequestStatus.Cancelled);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RentalRequest CreateRequestWithDetails()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var user = new User(
            Guid.NewGuid(),
            "testuser",
            "hash",
            "Test",
            "User",
            today.AddYears(-30),
            today.AddYears(-10),
            "test@example.com");

        var car = new Car(
            Guid.NewGuid(),
            "1HGCM82633A004352",
            "Toyota",
            "Camry",
            2022,
            CarCategory.Economy,
            80m,
            10_000);

        var request = new RentalRequest(Guid.NewGuid(), user.Id, car.Id, today.AddDays(1), today.AddDays(4));

        // Навигационные свойства заполняет EF Core, в юнит-тестах задаём их вручную.
        typeof(RentalRequest).GetProperty(nameof(RentalRequest.User))!.SetValue(request, user);
        typeof(RentalRequest).GetProperty(nameof(RentalRequest.Car))!.SetValue(request, car);

        return request;
    }

    private void SetupRequest(RentalRequest request)
    {
        _requestRepo
            .Setup(r => r.GetByIdWithDetailsAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
    }

    private RentalRequest CreateCancelledRequest()
    {
        RentalRequest request = CreateRequestWithDetails();
        request.Cancel(request.UserId);
        SetupRequest(request);
        return request;
    }
}
