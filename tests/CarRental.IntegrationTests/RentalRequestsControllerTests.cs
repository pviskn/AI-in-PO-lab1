using System.Net;
using System.Net.Http.Json;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;
using CarRental.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarRental.IntegrationTests;

public class RentalRequestsControllerTests : TestBase
{
    public RentalRequestsControllerTests(DatabaseFixture fixture) : base(fixture) { }


    [Fact]
    public async Task CreateRequest_Returns201_WhenCarIsAvailable()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_create_req");
        SetAuthToken(managerToken);
        var car = await (await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352")))
            .Content.ReadFromJsonAsync<CarDto>();
        ClearAuthToken();

        var clientToken = await RegisterAndGetTokenAsync("client_create_req");
        SetAuthToken(clientToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new CreateRentalRequestDto(car!.Id, today.AddDays(1), today.AddDays(4));

        var response = await Client.PostAsJsonAsync("/api/rental-requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RentalRequestDto>();
        body!.CarId.Should().Be(car.Id);
        body.Status.Should().Be(RentalRequestStatus.Pending);
    }

    [Fact]
    public async Task CreateRequest_Returns409_WhenPeriodOverlaps()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_overlap");
        SetAuthToken(managerToken);
        var car = await (await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("JH4KA7650MC002594")))
            .Content.ReadFromJsonAsync<CarDto>();
        ClearAuthToken();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var client1Token = await RegisterAndGetTokenAsync("client_overlap_1");
        SetAuthToken(client1Token);
        await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(car!.Id, today.AddDays(1), today.AddDays(5)));
        ClearAuthToken();

        var client2Token = await RegisterAndGetTokenAsync("client_overlap_2");
        SetAuthToken(client2Token);
        var response = await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(car.Id, today.AddDays(3), today.AddDays(7)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateRequest_Returns400_WhenExperienceInsufficientForSport()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_sport");
        SetAuthToken(managerToken);
        var car = await (await Client.PostAsJsonAsync("/api/cars",
                ValidCreateCarDto("2T1BURHE0JC051195", CarCategory.Sport)))
            .Content.ReadFromJsonAsync<CarDto>();
        ClearAuthToken();

        var clientToken = await RegisterAndGetTokenAsync("client_sport_new", experienceYears: 2);
        SetAuthToken(clientToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new CreateRentalRequestDto(car!.Id, today.AddDays(1), today.AddDays(4));

        var response = await Client.PostAsJsonAsync("/api/rental-requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task ApproveRequest_Returns200WithContract_WhenRequestIsPending()
    {
        var (requestId, managerToken) = await CreatePendingRequestAsync(
            "mgr_approve", "client_approve", "3VWFE21C04M000001");

        SetAuthToken(managerToken);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/approve", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contract = await response.Content.ReadFromJsonAsync<RentalContractDto>();
        contract!.RentalRequestId.Should().Be(requestId);
        contract.BasePrice.Should().BeGreaterThan(0);
    }


    [Fact]
    public async Task RejectRequest_Returns204_WhenRequestIsPending()
    {
        var (requestId, managerToken) = await CreatePendingRequestAsync(
            "mgr_reject", "client_reject", "1G1ZT53806F109149");

        SetAuthToken(managerToken);
        var response = await Client.PostAsJsonAsync(
            $"/api/rental-requests/{requestId}/reject",
            new RejectRentalRequestDto("Car unavailable for that period."));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }


    [Fact]
    public async Task CompleteRental_Returns200_WithZeroLateFee_WhenReturnedOnTime()
    {
        var (requestId, managerToken) = await CreateApprovedRequestAsync(
            "mgr_complete_ok", "client_complete_ok", "1FTFW1ET5EFC31160");

        SetAuthToken(managerToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new CompleteRentalDto(today.AddDays(4), 0m);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/complete", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contract = await response.Content.ReadFromJsonAsync<RentalContractDto>();
        contract!.LateFee.Should().Be(0m);
    }

    [Fact]
    public async Task CompleteRental_Returns200_WithPositiveLateFee_WhenReturnedLate()
    {
        var (requestId, managerToken) = await CreateApprovedRequestAsync(
            "mgr_complete_late", "client_complete_late", "1HGBH41JXMN109186");

        SetAuthToken(managerToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new CompleteRentalDto(today.AddDays(7), 0m);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/complete", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contract = await response.Content.ReadFromJsonAsync<RentalContractDto>();
        contract!.LateFee.Should().BeGreaterThan(0m);
    }


    [Fact]
    public async Task GetRequests_ReturnsOnlyOwnRequests_WhenCalledByClient()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_get_req");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("WBAPH5C58AA448978"));
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("WBAWV73589P473800"));
        var cars = await (await Client.GetAsync("/api/cars")).Content
            .ReadFromJsonAsync<PagedResult<CarDto>>();
        ClearAuthToken();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var clientAToken = await RegisterAndGetTokenAsync("client_get_a");
        SetAuthToken(clientAToken);
        await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(cars!.Items[0].Id, today.AddDays(1), today.AddDays(3)));
        ClearAuthToken();

        var clientBToken = await RegisterAndGetTokenAsync("client_get_b");
        SetAuthToken(clientBToken);
        await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(cars.Items[1].Id, today.AddDays(1), today.AddDays(3)));

        var response = await Client.GetAsync("/api/rental-requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        body!.Items.Should().AllSatisfy(r => r.ClientUsername.Should().Be("client_get_b"));
        body.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRequests_ReturnsAllRequests_WhenCalledByManager()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_get_all");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("WBAPH5C58AA448910"));
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("WBAWV73589P473811"));
        var cars = await (await Client.GetAsync("/api/cars")).Content
            .ReadFromJsonAsync<PagedResult<CarDto>>();
        ClearAuthToken();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var clientCToken = await RegisterAndGetTokenAsync("client_mgr_see_c");
        SetAuthToken(clientCToken);
        await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(cars!.Items[0].Id, today.AddDays(1), today.AddDays(3)));
        ClearAuthToken();

        var clientDToken = await RegisterAndGetTokenAsync("client_mgr_see_d");
        SetAuthToken(clientDToken);
        await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(cars.Items[1].Id, today.AddDays(1), today.AddDays(3)));
        ClearAuthToken();

        SetAuthToken(managerToken);
        var response = await Client.GetAsync("/api/rental-requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        body!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }


    [Fact]
    public async Task CancelRequest_Returns200AndPersistsCancelledStatus_WhenOwnerCancelsPendingRequest()
    {
        var (requestId, _) = await CreatePendingRequestAsync(
            "mgr_cancel_ok", "client_cancel_ok", "5YJSA1E26HF000001");

        var clientToken = await GetAuthTokenAsync("client_cancel_ok");
        SetAuthToken(clientToken);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/cancel", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RentalRequestDto>();
        body!.Id.Should().Be(requestId);
        body.Status.Should().Be(RentalRequestStatus.Cancelled);

        var listResponse = await Client.GetAsync("/api/rental-requests");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        list!.Items.Should().ContainSingle(r => r.Id == requestId)
            .Which.Status.Should().Be(RentalRequestStatus.Cancelled);
    }

    [Fact]
    public async Task CancelRequest_Returns400_WhenRequestBelongsToAnotherClient()
    {
        var (requestId, managerToken) = await CreatePendingRequestAsync(
            "mgr_cancel_owner", "client_cancel_owner", "5YJSA1E26HF000002");

        var otherClientToken = await RegisterAndGetTokenAsync("client_cancel_other");
        SetAuthToken(otherClientToken);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/cancel", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        SetAuthToken(managerToken);
        var list = await (await Client.GetAsync($"/api/rental-requests?status={RentalRequestStatus.Pending}"))
            .Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        list!.Items.Should().Contain(r => r.Id == requestId);
    }

    [Fact]
    public async Task CancelRequest_Returns400_WhenRequestIsNotPending()
    {
        var (requestId, _) = await CreateApprovedRequestAsync(
            "mgr_cancel_status", "client_cancel_status", "5YJSA1E26HF000003");

        var clientToken = await GetAuthTokenAsync("client_cancel_status");
        SetAuthToken(clientToken);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/cancel", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelRequest_Returns404_WhenRequestDoesNotExist()
    {
        var clientToken = await RegisterAndGetTokenAsync("client_cancel_missing");
        SetAuthToken(clientToken);

        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{Guid.NewGuid()}/cancel", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApproveRequest_Returns400_WhenRequestIsCancelled()
    {
        var (requestId, managerToken) = await CreateCancelledRequestAsync(
            "mgr_approve_cancelled", "client_approve_cancelled", "5YJSA1E26HF000004");

        SetAuthToken(managerToken);
        var response = await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/approve", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectRequest_Returns400_WhenRequestIsCancelled()
    {
        var (requestId, managerToken) = await CreateCancelledRequestAsync(
            "mgr_reject_cancelled", "client_reject_cancelled", "5YJSA1E26HF000005");

        SetAuthToken(managerToken);
        var response = await Client.PostAsJsonAsync(
            $"/api/rental-requests/{requestId}/reject",
            new RejectRentalRequestDto("Car unavailable for that period."));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteRental_Returns400_WhenRequestIsCancelled()
    {
        var (requestId, managerToken) = await CreateCancelledRequestAsync(
            "mgr_complete_cancelled", "client_complete_cancelled", "5YJSA1E26HF000006");

        SetAuthToken(managerToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var response = await Client.PostAsJsonAsync(
            $"/api/rental-requests/{requestId}/complete", new CompleteRentalDto(today.AddDays(4), 0m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    private static CreateCarDto ValidCreateCarDto(
        string vin,
        CarCategory category = CarCategory.Economy,
        decimal pricePerDay  = 80m)
        => new(vin, "Toyota", "Camry", 2022, category, pricePerDay, 10_000);

    private async Task<(Guid requestId, string managerToken)> CreatePendingRequestAsync(
        string managerUsername, string clientUsername, string vin)
    {
        var managerToken = await CreateManagerAndGetTokenAsync(managerUsername);
        SetAuthToken(managerToken);
        var car = await (await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto(vin)))
            .Content.ReadFromJsonAsync<CarDto>();
        ClearAuthToken();

        var clientToken = await RegisterAndGetTokenAsync(clientUsername);
        SetAuthToken(clientToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = await (await Client.PostAsJsonAsync("/api/rental-requests",
                new CreateRentalRequestDto(car!.Id, today.AddDays(1), today.AddDays(4))))
            .Content.ReadFromJsonAsync<RentalRequestDto>();
        ClearAuthToken();

        return (request!.Id, managerToken);
    }

    private async Task<(Guid requestId, string managerToken)> CreateApprovedRequestAsync(
        string managerUsername, string clientUsername, string vin)
    {
        var (requestId, managerToken) = await CreatePendingRequestAsync(managerUsername, clientUsername, vin);

        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/approve", new { });
        ClearAuthToken();

        return (requestId, managerToken);
    }

    private async Task<(Guid requestId, string managerToken)> CreateCancelledRequestAsync(
        string managerUsername, string clientUsername, string vin)
    {
        var (requestId, managerToken) = await CreatePendingRequestAsync(managerUsername, clientUsername, vin);

        var clientToken = await GetAuthTokenAsync(clientUsername);
        SetAuthToken(clientToken);
        await Client.PostAsJsonAsync($"/api/rental-requests/{requestId}/cancel", new { });
        ClearAuthToken();

        return (requestId, managerToken);
    }
}
