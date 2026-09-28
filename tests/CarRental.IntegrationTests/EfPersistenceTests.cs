using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.IntegrationTests;

public class EfPersistenceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }


    [Fact]
    public async Task Car_CanBeSavedAndRetrieved_UsingEfCore()
    {
        var id  = Guid.NewGuid();
        var car = new Car(id, "1HGCM82633A004352", "Toyota", "Camry", 2022,
            CarCategory.Economy, 70m, 15_000);

        await using (var ctx = CreateContext(nameof(Car_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            ctx.Cars.Add(car);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(Car_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            var loaded = await ctx.Cars.FindAsync(id);

            loaded.Should().NotBeNull();
            loaded!.Vin.Value.Should().Be("1HGCM82633A004352");
            loaded.Make.Should().Be("Toyota");
            loaded.Model.Should().Be("Camry");
            loaded.Year.Should().Be(2022);
            loaded.Category.Should().Be(CarCategory.Economy);
            loaded.DailyRate.Should().Be(70m);
            loaded.Mileage.Should().Be(15_000);
            loaded.Status.Should().Be(CarStatus.Available);
        }
    }

    [Fact]
    public async Task Car_SavesStatusChange_AfterRent()
    {
        var id  = Guid.NewGuid();
        var car = new Car(id, "JH4KA7650MC002594", "Honda", "Civic", 2021,
            CarCategory.Standard, 80m, 20_000);

        await using (var ctx = CreateContext(nameof(Car_SavesStatusChange_AfterRent)))
        {
            ctx.Cars.Add(car);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(Car_SavesStatusChange_AfterRent)))
        {
            var loaded = await ctx.Cars.FindAsync(id);
            loaded!.Rent();
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(Car_SavesStatusChange_AfterRent)))
        {
            var loaded = await ctx.Cars.FindAsync(id);
            loaded!.Status.Should().Be(CarStatus.Rented);
        }
    }

    [Fact]
    public async Task Car_UniqueVin_IsEnforcedByEf()
    {
        const string vin = "WVWZZZ1JZ3W386752";
        var car1 = new Car(Guid.NewGuid(), vin, "VW", "Golf", 2020, CarCategory.Economy, 60m, 0);
        var car2 = new Car(Guid.NewGuid(), vin, "VW", "Polo", 2021, CarCategory.Economy, 55m, 0);

        await using var ctx = CreateContext(nameof(Car_UniqueVin_IsEnforcedByEf));
        ctx.Cars.Add(car1);
        ctx.Cars.Add(car2);

        Func<Task> act = () => ctx.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }


    [Fact]
    public async Task User_CanBeSavedAndRetrieved_UsingEfCore()
    {
        var id      = Guid.NewGuid();
        var today   = DateOnly.FromDateTime(DateTime.Today);
        var user    = new User(id, "john_doe", "hashedpassword",
            "John", "Doe", today.AddYears(-25), today.AddYears(-5), "john@example.com");

        await using (var ctx = CreateContext(nameof(User_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(User_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            var loaded = await ctx.Users.FindAsync(id);

            loaded.Should().NotBeNull();
            loaded!.Username.Should().Be("john_doe");
            loaded.FirstName.Should().Be("John");
            loaded.LastName.Should().Be("Doe");
            loaded.Email.Should().Be("john@example.com");
            loaded.PasswordHash.Should().Be("hashedpassword");
        }
    }

    [Fact]
    public async Task User_WithRole_CanBeSavedAndRetrieved()
    {
        var userId  = Guid.NewGuid();
        var roleId  = Guid.NewGuid();
        var today   = DateOnly.FromDateTime(DateTime.Today);
        var user    = new User(userId, "manager1", "hash",
            "Alice", "Smith", today.AddYears(-30), today.AddYears(-10), "alice@example.com");
        var role    = new Role(roleId, UserRole.Manager);
        user.AddRole(role);

        await using (var ctx = CreateContext(nameof(User_WithRole_CanBeSavedAndRetrieved)))
        {
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(User_WithRole_CanBeSavedAndRetrieved)))
        {
            var loaded = await ctx.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            loaded.Should().NotBeNull();
            loaded!.Roles.Should().HaveCount(1);
            loaded.Roles.First().Name.Should().Be(UserRole.Manager);
        }
    }


    [Fact]
    public async Task RentalRequest_CanBeSavedAndRetrieved_UsingEfCore()
    {
        var carId     = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var today     = DateOnly.FromDateTime(DateTime.Today);

        var car  = new Car(carId, "1HGCM82633A004352", "BMW", "X5", 2023,
            CarCategory.Premium, 200m, 0);
        var user = new User(userId, "client1", "hash",
            "Bob", "Jones", today.AddYears(-30), today.AddYears(-10), "bob@example.com");
        var request = new RentalRequest(requestId, userId, carId,
            today.AddDays(1), today.AddDays(8));

        await using (var ctx = CreateContext(nameof(RentalRequest_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            ctx.Cars.Add(car);
            ctx.Users.Add(user);
            ctx.RentalRequests.Add(request);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(RentalRequest_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            var loaded = await ctx.RentalRequests.FindAsync(requestId);

            loaded.Should().NotBeNull();
            loaded!.Status.Should().Be(RentalRequestStatus.Pending);
            loaded.CarId.Should().Be(carId);
            loaded.UserId.Should().Be(userId);
            loaded.StartDate.Should().Be(today.AddDays(1));
            loaded.EndDate.Should().Be(today.AddDays(8));
        }
    }

    [Fact]
    public async Task RentalRequest_StatusChange_IsPersisted()
    {
        var carId     = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var today     = DateOnly.FromDateTime(DateTime.Today);

        var car     = new Car(carId, "JH4KA7650MC002594", "Audi", "A4", 2022,
            CarCategory.Standard, 100m, 0);
        var user    = new User(userId, "client2", "hash",
            "Eve", "Brown", today.AddYears(-25), today.AddYears(-5), "eve@example.com");
        var request = new RentalRequest(requestId, userId, carId,
            today.AddDays(2), today.AddDays(9));

        await using (var ctx = CreateContext(nameof(RentalRequest_StatusChange_IsPersisted)))
        {
            ctx.Cars.Add(car);
            ctx.Users.Add(user);
            ctx.RentalRequests.Add(request);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(RentalRequest_StatusChange_IsPersisted)))
        {
            var loaded = await ctx.RentalRequests.FindAsync(requestId);
            loaded!.Approve();
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(RentalRequest_StatusChange_IsPersisted)))
        {
            var loaded = await ctx.RentalRequests.FindAsync(requestId);
            loaded!.Status.Should().Be(RentalRequestStatus.Approved);
        }
    }

    [Fact]
    public async Task RentalRequest_NavigationProperties_LoadWithInclude()
    {
        var carId     = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var today     = DateOnly.FromDateTime(DateTime.Today);

        var car     = new Car(carId, "WVWZZZ1JZ3W386752", "Mercedes", "C200", 2023,
            CarCategory.Premium, 180m, 0);
        var user    = new User(userId, "client3", "hash",
            "Max", "Weber", today.AddYears(-28), today.AddYears(-8), "max@example.com");
        var request = new RentalRequest(requestId, userId, carId,
            today.AddDays(3), today.AddDays(10));

        await using (var ctx = CreateContext(nameof(RentalRequest_NavigationProperties_LoadWithInclude)))
        {
            ctx.Cars.Add(car);
            ctx.Users.Add(user);
            ctx.RentalRequests.Add(request);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(RentalRequest_NavigationProperties_LoadWithInclude)))
        {
            var loaded = await ctx.RentalRequests
                .Include(r => r.Car)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            loaded.Should().NotBeNull();
            loaded!.Car.Should().NotBeNull();
            loaded.Car.Make.Should().Be("Mercedes");
            loaded.User.Should().NotBeNull();
            loaded.User.Username.Should().Be("client3");
        }
    }


    [Fact]
    public async Task RentalContract_CanBeSavedAndRetrieved_UsingEfCore()
    {
        var carId      = Guid.NewGuid();
        var userId     = Guid.NewGuid();
        var requestId  = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var today      = DateOnly.FromDateTime(DateTime.Today);

        var car      = new Car(carId, "1HGCM82633A004352", "Toyota", "RAV4", 2022,
            CarCategory.Standard, 90m, 5_000);
        var user     = new User(userId, "client4", "hash",
            "Ann", "Lee", today.AddYears(-27), today.AddYears(-7), "ann@example.com");
        var request  = new RentalRequest(requestId, userId, carId,
            today.AddDays(1), today.AddDays(6));
        request.Approve();
        var contract = new RentalContract(contractId, requestId, 450m);

        await using (var ctx = CreateContext(nameof(RentalContract_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            ctx.Cars.Add(car);
            ctx.Users.Add(user);
            ctx.RentalRequests.Add(request);
            ctx.RentalContracts.Add(contract);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(RentalContract_CanBeSavedAndRetrieved_UsingEfCore)))
        {
            var loaded = await ctx.RentalContracts.FindAsync(contractId);

            loaded.Should().NotBeNull();
            loaded!.BasePrice.Should().Be(450m);
            loaded.TotalPrice.Should().Be(450m);
            loaded.LateFee.Should().Be(0m);
            loaded.ActualReturnDate.Should().BeNull();
        }
    }

    [Fact]
    public async Task RentalContract_Complete_PersistsFeesAndReturnDate()
    {
        var carId      = Guid.NewGuid();
        var userId     = Guid.NewGuid();
        var requestId  = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var today      = DateOnly.FromDateTime(DateTime.Today);

        var car      = new Car(carId, "JH4KA7650MC002594", "Lexus", "RX", 2021,
            CarCategory.Premium, 150m, 3_000);
        var user     = new User(userId, "client5", "hash",
            "Sam", "Park", today.AddYears(-32), today.AddYears(-10), "sam@example.com");
        var request  = new RentalRequest(requestId, userId, carId,
            today.AddDays(1), today.AddDays(6));
        request.Approve();
        var contract = new RentalContract(contractId, requestId, 750m);

        await using (var ctx = CreateContext(nameof(RentalContract_Complete_PersistsFeesAndReturnDate)))
        {
            ctx.Cars.Add(car);
            ctx.Users.Add(user);
            ctx.RentalRequests.Add(request);
            ctx.RentalContracts.Add(contract);
            await ctx.SaveChangesAsync();
        }

        var returnDate = today.AddDays(8);
        await using (var ctx = CreateContext(nameof(RentalContract_Complete_PersistsFeesAndReturnDate)))
        {
            var loaded = await ctx.RentalContracts.FindAsync(contractId);
            loaded!.Complete(returnDate, 450m, 100m);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(nameof(RentalContract_Complete_PersistsFeesAndReturnDate)))
        {
            var loaded = await ctx.RentalContracts.FindAsync(contractId);
            loaded!.ActualReturnDate.Should().Be(returnDate);
            loaded.LateFee.Should().Be(450m);
            loaded.DamageFee.Should().Be(100m);
            loaded.TotalPrice.Should().Be(1300m);
        }
    }
}
