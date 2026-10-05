using System.Net;
using System.Net.Http.Json;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;
using CarRental.Infrastructure.Persistence;
using CarRental.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarRental.IntegrationTests;

public class CarsControllerTests : TestBase
{
    public CarsControllerTests(DatabaseFixture fixture) : base(fixture) { }


    [Fact]
    public async Task GetCars_Returns200WithEmptyList_WhenNoCarsExist()
    {
        var response = await Client.GetAsync("/api/cars");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CarDto>>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCars_ReturnsCars_AfterOneIsAdded()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_get");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));
        ClearAuthToken();

        var response = await Client.GetAsync("/api/cars");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<CarDto>>();
        body!.Items.Should().HaveCount(1);
        body.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCars_FiltersByCategory()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_filter");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352", CarCategory.Economy));
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("JH4KA7650MC002594", CarCategory.Sport));
        ClearAuthToken();

        var response = await Client.GetAsync("/api/cars?category=Economy");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<CarDto>>();
        body!.Items.Should().HaveCount(1);
        body.Items[0].Category.Should().Be(CarCategory.Economy);
    }

    [Fact]
    public async Task GetCars_FiltersByPriceRange()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_price");
        SetAuthToken(managerToken);
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352", pricePerDay: 50));
        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("JH4KA7650MC002594", pricePerDay: 200));
        ClearAuthToken();

        var response = await Client.GetAsync("/api/cars?maxPrice=100");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<CarDto>>();
        body!.Items.Should().HaveCount(1);
        body.Items[0].PricePerDay.Should().Be(50);
    }


    [Fact]
    public async Task GetCarById_Returns200_WhenCarExists()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_byid");
        SetAuthToken(managerToken);
        var created = await (await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352")))
            .Content.ReadFromJsonAsync<CarDto>();
        ClearAuthToken();

        var response = await Client.GetAsync($"/api/cars/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CarDto>();
        body!.Id.Should().Be(created.Id);
        body.Vin.Should().Be("1HGCM82633A004352");
    }

    [Fact]
    public async Task GetCarById_Returns404_WhenCarDoesNotExist()
    {
        var response = await Client.GetAsync($"/api/cars/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task AddCar_Returns401_WhenNotAuthenticated()
    {
        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddCar_Returns403_WhenUserIsClient()
    {
        var clientToken = await RegisterAndGetTokenAsync("client_addcar");
        SetAuthToken(clientToken);

        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddCar_Returns201_WhenManagerAddsValidCar()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_add");
        SetAuthToken(managerToken);

        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CarDto>();
        body!.Vin.Should().Be("1HGCM82633A004352");
        body.Status.Should().Be(CarStatus.Available);
    }

    [Fact]
    public async Task AddCar_Returns409_WhenVinAlreadyExists()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_dup_vin");
        SetAuthToken(managerToken);

        await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));
        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }


    [Fact]
    public async Task ChangeCarStatus_Returns204_WhenManagerChangesToMaintenance()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_status");
        SetAuthToken(managerToken);
        var car = await (await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352")))
            .Content.ReadFromJsonAsync<CarDto>();

        var response = await Client.PatchAsJsonAsync($"/api/cars/{car!.Id}/status",
            new ChangeCarStatusDto(CarStatus.UnderMaintenance));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeCarStatus_Returns401_WhenNotAuthenticated()
    {
        var response = await Client.PatchAsJsonAsync($"/api/cars/{Guid.NewGuid()}/status",
            new ChangeCarStatusDto(CarStatus.UnderMaintenance));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task DeleteCar_Returns204_AndKeepsCarInDatabase()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");

        var response = await Client.DeleteAsync($"/api/cars/{car.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await ctx.Cars.FindAsync(car.Id);
        stored.Should().NotBeNull();
        stored!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCar_Returns204_WhenAdminDeletesCar()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_by_admin");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");

        SetAuthToken(await CreateAdminAndGetTokenAsync("admin_delete"));
        var response = await Client.DeleteAsync($"/api/cars/{car.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletedCar_IsExcludedFromCarList()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_list");
        SetAuthToken(managerToken);
        var deleted = await CreateCarAsync("1HGCM82633A004352");
        var kept = await CreateCarAsync("JH4KA7650MC002594");
        await Client.DeleteAsync($"/api/cars/{deleted.Id}");
        ClearAuthToken();

        var response = await Client.GetAsync("/api/cars");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<CarDto>>();
        body!.Items.Should().ContainSingle().Which.Id.Should().Be(kept.Id);
        body.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCarById_Returns404_WhenCarIsDeleted()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_byid");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");
        await Client.DeleteAsync($"/api/cars/{car.Id}");
        ClearAuthToken();

        var response = await Client.GetAsync($"/api/cars/{car.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeCarStatus_Returns404_WhenCarIsDeleted()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_status");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");
        await Client.DeleteAsync($"/api/cars/{car.Id}");

        var response = await Client.PatchAsJsonAsync($"/api/cars/{car.Id}/status",
            new ChangeCarStatusDto(CarStatus.UnderMaintenance));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCar_Returns400_WhenCarIsAlreadyDeleted()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_twice");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");
        await Client.DeleteAsync($"/api/cars/{car.Id}");

        var response = await Client.DeleteAsync($"/api/cars/{car.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCar_Returns404_WhenCarDoesNotExist()
    {
        SetAuthToken(await CreateManagerAndGetTokenAsync("mgr_delete_missing"));

        var response = await Client.DeleteAsync($"/api/cars/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCar_Returns401_WhenNotAuthenticated()
    {
        var response = await Client.DeleteAsync($"/api/cars/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteCar_Returns403_WhenUserIsClient()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_client");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");

        SetAuthToken(await RegisterAndGetTokenAsync("client_delete"));
        var response = await Client.DeleteAsync($"/api/cars/{car.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RestoreCar_Returns204_AndCarIsVisibleAgain()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_restore");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");
        await Client.DeleteAsync($"/api/cars/{car.Id}");

        var response = await Client.PostAsync($"/api/cars/{car.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ClearAuthToken();

        var byId = await Client.GetAsync($"/api/cars/{car.Id}");
        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        var restored = await byId.Content.ReadFromJsonAsync<CarDto>();
        restored.Should().BeEquivalentTo(car);

        var list = await (await Client.GetAsync("/api/cars")).Content.ReadFromJsonAsync<PagedResult<CarDto>>();
        list!.Items.Should().ContainSingle().Which.Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task RestoreCar_Returns400_WhenCarIsNotDeleted()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_restore_active");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");

        var response = await Client.PostAsync($"/api/cars/{car.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RestoreCar_Returns404_WhenCarDoesNotExist()
    {
        SetAuthToken(await CreateManagerAndGetTokenAsync("mgr_restore_missing"));

        var response = await Client.PostAsync($"/api/cars/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreCar_Returns401_WhenNotAuthenticated()
    {
        var response = await Client.PostAsync($"/api/cars/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RestoreCar_Returns403_WhenUserIsClient()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_restore_client");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");
        await Client.DeleteAsync($"/api/cars/{car.Id}");

        SetAuthToken(await RegisterAndGetTokenAsync("client_restore"));
        var response = await Client.PostAsync($"/api/cars/{car.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddCar_Returns409_WhenVinBelongsToDeletedCar()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_delete_vin");
        SetAuthToken(managerToken);
        var car = await CreateCarAsync("1HGCM82633A004352");
        await Client.DeleteAsync($"/api/cars/{car.Id}");

        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto("1HGCM82633A004352"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }


    private static CreateCarDto ValidCreateCarDto(
        string vin,
        CarCategory category = CarCategory.Economy,
        decimal pricePerDay  = 80m)
        => new(vin, "Toyota", "Camry", 2022, category, pricePerDay, 10_000);

    private async Task<CarDto> CreateCarAsync(string vin)
    {
        var response = await Client.PostAsJsonAsync("/api/cars", ValidCreateCarDto(vin));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CarDto>())!;
    }
}
