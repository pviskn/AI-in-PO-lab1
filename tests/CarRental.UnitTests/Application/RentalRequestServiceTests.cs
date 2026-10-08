using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.DTOs;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
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
    public async Task CancelRequestAsync_ShouldCancelAndSave_WhenOwnerCancelsPendingRequest()
    {
        RentalRequest request = SetupRequest();

        await _service.CancelRequestAsync(request.Id, request.UserId);

        request.Status.Should().Be(RentalRequestStatus.Cancelled);
        _requestRepo.Verify(r => r.Update(request), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldThrowNotOwned_WhenUserIsNotOwner()
    {
        RentalRequest request = SetupRequest();

        Func<Task> act = () => _service.CancelRequestAsync(request.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<RentalRequestNotOwnedException>();
        request.Status.Should().Be(RentalRequestStatus.Pending);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldThrowNotCancellable_WhenRequestIsNotPending()
    {
        RentalRequest request = SetupRequest();
        request.Approve();

        Func<Task> act = () => _service.CancelRequestAsync(request.Id, request.UserId);

        await act.Should().ThrowAsync<RentalRequestNotCancellableException>();
        request.Status.Should().Be(RentalRequestStatus.Approved);
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
    public async Task ApproveRequestAsync_ShouldThrow_WhenRequestIsCancelled()
    {
        RentalRequest request = SetupCancelledRequest();

        Func<Task> act = () => _service.ApproveRequestAsync(request.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectRequestAsync_ShouldThrow_WhenRequestIsCancelled()
    {
        RentalRequest request = SetupCancelledRequest();

        Func<Task> act = () => _service.RejectRequestAsync(request.Id, "причина");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteRentalAsync_ShouldThrow_WhenRequestIsCancelled()
    {
        RentalRequest request = SetupCancelledRequest();
        var dto = new CompleteRentalDto(DateOnly.FromDateTime(DateTime.Today).AddDays(4), 0m);

        Func<Task> act = () => _service.CompleteRentalAsync(request.Id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private RentalRequest SetupRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = new RentalRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), today.AddDays(1), today.AddDays(4));
        _requestRepo
            .Setup(r => r.GetByIdWithDetailsAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        return request;
    }

    private RentalRequest SetupCancelledRequest()
    {
        RentalRequest request = SetupRequest();
        request.Cancel(request.UserId);
        return request;
    }
}
