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
    public async Task GetRequests_Returns401_WhenNotAuthenticated()
    {
        var response = await Client.GetAsync("/api/rental-requests");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRequests_ReturnsRequestedPage_WhenPaginationSpecified()
    {
        var (_, managerToken) = await CreateClientRequestsAsync(
            "mgr_page", "client_page", "1HGCM82633A004352", "WBAPH5C58AA448978", "WBAWV73589P473800");
        SetAuthToken(managerToken);

        var firstResponse = await Client.GetAsync("/api/rental-requests?page=1&pageSize=2");
        var secondResponse = await Client.GetAsync("/api/rental-requests?page=2&pageSize=2");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await firstResponse.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        var second = await secondResponse.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();

        first!.Items.Should().HaveCount(2);
        first.Page.Should().Be(1);
        first.PageSize.Should().Be(2);
        first.TotalCount.Should().Be(3);
        first.TotalPages.Should().Be(2);
        first.HasPreviousPage.Should().BeFalse();
        first.HasNextPage.Should().BeTrue();

        second!.Items.Should().ContainSingle();
        second.Page.Should().Be(2);
        second.HasPreviousPage.Should().BeTrue();
        second.HasNextPage.Should().BeFalse();

        first.Items.Select(r => r.Id).Should().NotIntersectWith(second.Items.Select(r => r.Id));
    }

    [Fact]
    public async Task GetRequests_ReturnsEmptyItems_WhenPageIsBeyondLastPage()
    {
        var (_, managerToken) = await CreateClientRequestsAsync(
            "mgr_page_out", "client_page_out", "1HGCM82633A004352");
        SetAuthToken(managerToken);

        var response = await Client.GetAsync("/api/rental-requests?page=5&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(1);
        body.Page.Should().Be(5);
    }

    [Fact]
    public async Task GetRequests_NormalizesPagination_WhenValuesOutOfRange()
    {
        var (_, managerToken) = await CreateClientRequestsAsync(
            "mgr_page_norm", "client_page_norm", "1HGCM82633A004352");
        SetAuthToken(managerToken);

        var response = await Client.GetAsync("/api/rental-requests?page=0&pageSize=1000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(100);
        body.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetRequests_ReturnsOnlyRequestsWithStatus_WhenStatusFilterSpecified()
    {
        var (requestIds, managerToken) = await CreateClientRequestsAsync(
            "mgr_status_filter", "client_status_filter",
            "1HGCM82633A004352", "WBAPH5C58AA448978", "WBAWV73589P473800");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync($"/api/rental-requests/{requestIds[0]}/approve", new { });
        await Client.PostAsJsonAsync($"/api/rental-requests/{requestIds[1]}/reject",
            new RejectRentalRequestDto("Not available"));

        var approved = await GetRequestsAsync("/api/rental-requests?status=Approved");
        var rejected = await GetRequestsAsync("/api/rental-requests?status=Rejected");
        var pending = await GetRequestsAsync("/api/rental-requests?status=Pending");
        var completed = await GetRequestsAsync("/api/rental-requests?status=Completed");

        approved.Items.Should().ContainSingle().Which.Id.Should().Be(requestIds[0]);
        approved.Items[0].Status.Should().Be(RentalRequestStatus.Approved);
        rejected.Items.Should().ContainSingle().Which.Id.Should().Be(requestIds[1]);
        rejected.Items[0].ManagerComment.Should().Be("Not available");
        pending.Items.Should().ContainSingle().Which.Id.Should().Be(requestIds[2]);
        completed.Items.Should().BeEmpty();
        completed.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRequests_CombinesStatusFilterAndPagination()
    {
        var (_, managerToken) = await CreateClientRequestsAsync(
            "mgr_status_page", "client_status_page",
            "1HGCM82633A004352", "WBAPH5C58AA448978", "WBAWV73589P473800");
        SetAuthToken(managerToken);

        var body = await GetRequestsAsync("/api/rental-requests?status=Pending&page=2&pageSize=2");

        body.Items.Should().ContainSingle();
        body.Items.Should().AllSatisfy(r => r.Status.Should().Be(RentalRequestStatus.Pending));
        body.TotalCount.Should().Be(3);
        body.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetRequests_AppliesStatusFilterToOwnRequestsOnly_WhenCalledByClient()
    {
        var (requestIds, managerToken) = await CreateClientRequestsAsync(
            "mgr_client_status", "client_status_own", "1HGCM82633A004352", "WBAPH5C58AA448978");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync($"/api/rental-requests/{requestIds[0]}/approve", new { });
        ClearAuthToken();

        var otherCar = await CreateCarAsManagerAsync(managerToken, "WBAWV73589P473800");
        var otherClientToken = await RegisterAndGetTokenAsync("client_status_other");
        SetAuthToken(otherClientToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        await Client.PostAsJsonAsync("/api/rental-requests",
            new CreateRentalRequestDto(otherCar.Id, today.AddDays(1), today.AddDays(3)));

        var body = await GetRequestsAsync("/api/rental-requests?status=Pending");

        body.Items.Should().ContainSingle();
        body.Items[0].ClientUsername.Should().Be("client_status_other");
        body.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRequests_Returns400_WhenStatusIsInvalid()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_bad_status");
        SetAuthToken(managerToken);

        var response = await Client.GetAsync("/api/rental-requests?status=Unknown");

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

    private async Task<PagedResult<RentalRequestDto>> GetRequestsAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<RentalRequestDto>>())!;
    }

    private async Task<CarDto> CreateCarAsManagerAsync(string managerToken, string vin)
    {
        SetAuthToken(managerToken);
        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto(vin));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ClearAuthToken();
        return (await response.Content.ReadFromJsonAsync<CarDto>())!;
    }

    private async Task<(List<Guid> requestIds, string managerToken)> CreateClientRequestsAsync(
        string managerUsername, string clientUsername, params string[] vins)
    {
        var managerToken = await CreateManagerAndGetTokenAsync(managerUsername);
        var cars = new List<CarDto>();
        foreach (var vin in vins)
            cars.Add(await CreateCarAsManagerAsync(managerToken, vin));

        var clientToken = await RegisterAndGetTokenAsync(clientUsername);
        SetAuthToken(clientToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var requestIds = new List<Guid>();
        foreach (var car in cars)
        {
            var response = await Client.PostAsJsonAsync("/api/rental-requests",
                new CreateRentalRequestDto(car.Id, today.AddDays(1), today.AddDays(3)));
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            requestIds.Add((await response.Content.ReadFromJsonAsync<RentalRequestDto>())!.Id);
        }

        ClearAuthToken();
        return (requestIds, managerToken);
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
}
