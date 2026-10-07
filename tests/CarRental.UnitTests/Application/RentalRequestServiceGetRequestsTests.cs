using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using FluentAssertions;
using Moq;
using System.Reflection;
using Xunit;

namespace CarRental.UnitTests.Application;

public class RentalRequestServiceGetRequestsTests
{
    private const string ValidVin = "1HGCM82633A004352";

    private readonly Mock<IRentalRequestRepository> _requestRepo = new();

    private readonly RentalRequestService _service;

    public RentalRequestServiceGetRequestsTests()
    {
        _service = new RentalRequestService(
            _requestRepo.Object,
            Mock.Of<IRentalContractRepository>(),
            Mock.Of<ICarRepository>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IRentalEligibilityService>(),
            Mock.Of<IRentalPricingService>());
    }

    [Fact]
    public async Task GetRequestsAsync_ShouldFilterByCurrentUser_WhenCallerIsClient()
    {
        var userId = Guid.NewGuid();
        SetupRepository(_requestRepo, Array.Empty<RentalRequest>(), 0);

        await _service.GetRequestsAsync(userId, false, null, 1, 20);

        _requestRepo.Verify(
            r => r.GetPagedAsync(userId, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRequestsAsync_ShouldNotFilterByUser_WhenCallerIsManager()
    {
        SetupRepository(_requestRepo, Array.Empty<RentalRequest>(), 0);

        await _service.GetRequestsAsync(Guid.NewGuid(), true, null, 1, 20);

        _requestRepo.Verify(
            r => r.GetPagedAsync(null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(RentalRequestStatus.Pending)]
    [InlineData(RentalRequestStatus.Approved)]
    [InlineData(RentalRequestStatus.Rejected)]
    [InlineData(RentalRequestStatus.Completed)]
    public async Task GetRequestsAsync_ShouldPassStatusFilterToRepository(RentalRequestStatus status)
    {
        SetupRepository(_requestRepo, Array.Empty<RentalRequest>(), 0);

        await _service.GetRequestsAsync(Guid.NewGuid(), true, status, 1, 20);

        _requestRepo.Verify(
            r => r.GetPagedAsync(null, status, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 20, 1, 20)]
    [InlineData(3, 0, 3, 1)]
    [InlineData(2, 500, 2, 100)]
    [InlineData(4, 15, 4, 15)]
    public async Task GetRequestsAsync_ShouldNormalizePagination(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        SetupRepository(_requestRepo, Array.Empty<RentalRequest>(), 0);

        PagedResult<RentalRequestDto> result = await _service.GetRequestsAsync(
            Guid.NewGuid(), true, null, page, pageSize);

        _requestRepo.Verify(
            r => r.GetPagedAsync(null, null, expectedPage, expectedPageSize, It.IsAny<CancellationToken>()),
            Times.Once);
        result.Page.Should().Be(expectedPage);
        result.PageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public async Task GetRequestsAsync_ShouldReturnMappedDtosWithPagingMetadata()
    {
        RentalRequest request = CreateRequestWithDetails("client_a");
        SetupRepository(_requestRepo, new[] { request }, 5);

        PagedResult<RentalRequestDto> result = await _service.GetRequestsAsync(
            request.UserId, false, null, 2, 2);

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.Items.Should().ContainSingle();

        RentalRequestDto dto = result.Items[0];
        dto.Id.Should().Be(request.Id);
        dto.ClientId.Should().Be(request.UserId);
        dto.ClientUsername.Should().Be("client_a");
        dto.CarId.Should().Be(request.CarId);
        dto.CarVin.Should().Be(ValidVin);
        dto.CarMake.Should().Be("Toyota");
        dto.CarModel.Should().Be("Camry");
        dto.StartDate.Should().Be(request.StartDate);
        dto.EndDate.Should().Be(request.EndDate);
        dto.Status.Should().Be(RentalRequestStatus.Pending);
    }

    private static void SetupRepository(
        Mock<IRentalRequestRepository> requestRepo,
        IReadOnlyList<RentalRequest> items,
        int totalCount)
    {
        requestRepo
            .Setup(r => r.GetPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<RentalRequestStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, totalCount));
    }

    private static RentalRequest CreateRequestWithDetails(string username)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var user = new User(
            Guid.NewGuid(),
            username,
            "hash",
            "Test",
            "User",
            today.AddYears(-30),
            today.AddYears(-10),
            "test@example.com");
        var car = new Car(Guid.NewGuid(), ValidVin, "Toyota", "Camry", 2020, CarCategory.Economy, 50m, 10_000);
        var request = new RentalRequest(Guid.NewGuid(), user.Id, car.Id, today.AddDays(1), today.AddDays(3));

        // Navigation properties are populated by EF Core in production; set them directly here.
        SetProperty(request, nameof(RentalRequest.User), user);
        SetProperty(request, nameof(RentalRequest.Car), car);

        return request;
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName);
        if (property is null)
            throw new InvalidOperationException($"Property {propertyName} not found.");

        property.SetValue(target, value);
    }
}
