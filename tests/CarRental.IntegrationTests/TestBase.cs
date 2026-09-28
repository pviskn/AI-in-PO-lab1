using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.DTOs;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Infrastructure.Persistence;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarRental.IntegrationTests;

[Collection("Integration")]
public abstract class TestBase : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected TestBase(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor is not null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseNpgsql(_fixture.Container.GetConnectionString()));
                });
            });

        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ctx.Database.EnsureCreatedAsync();
        await SeedRolesAsync(ctx);
    }

    public virtual async Task DisposeAsync()
    {
        await ClearDataAsync();
        Client.Dispose();
        await Factory.DisposeAsync();
    }


    protected async Task<string> RegisterAndGetTokenAsync(
        string username, string password = "Password123!",
        int ageYears = 25, int experienceYears = 5)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new RegisterDto(username, password,
            "Test", "User", $"{username}@test.com",
            today.AddYears(-ageYears), today.AddYears(-experienceYears));

        var response = await Client.PostAsJsonAsync("/api/auth/register", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.AccessToken;
    }

    protected async Task<string> GetAuthTokenAsync(string username, string password = "Password123!")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(username, password));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.AccessToken;
    }

    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected async Task<string> CreateAdminAndGetTokenAsync(
        string username = "admin_user", string password = "Admin123!")
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);
        var adminRole  = await ctx.Roles.FirstAsync(r => r.Name == UserRole.Admin);
        var clientRole = await ctx.Roles.FirstAsync(r => r.Name == UserRole.Client);

        var admin = new User(Guid.NewGuid(), username, passwordHash,
            "Admin", "User", today.AddYears(-30), today.AddYears(-10), $"{username}@test.com");
        admin.AddRole(clientRole);
        admin.AddRole(adminRole);

        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();

        return await GetAuthTokenAsync(username, password);
    }

    protected async Task<string> CreateManagerAndGetTokenAsync(
        string username = "manager_user", string password = "Manager123!")
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);
        var managerRole = await ctx.Roles.FirstAsync(r => r.Name == UserRole.Manager);
        var clientRole  = await ctx.Roles.FirstAsync(r => r.Name == UserRole.Client);

        var manager = new User(Guid.NewGuid(), username, passwordHash,
            "Manager", "User", today.AddYears(-30), today.AddYears(-10), $"{username}@test.com");
        manager.AddRole(clientRole);
        manager.AddRole(managerRole);

        ctx.Users.Add(manager);
        await ctx.SaveChangesAsync();

        return await GetAuthTokenAsync(username, password);
    }


    private static async Task SeedRolesAsync(ApplicationDbContext ctx)
    {
        if (!await ctx.Roles.AnyAsync())
        {
            ctx.Roles.AddRange(
                new Role(Guid.NewGuid(), UserRole.Client),
                new Role(Guid.NewGuid(), UserRole.Manager),
                new Role(Guid.NewGuid(), UserRole.Admin));
            await ctx.SaveChangesAsync();
        }
    }

    private async Task ClearDataAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        ctx.RentalContracts.RemoveRange(ctx.RentalContracts);
        ctx.RentalRequests.RemoveRange(ctx.RentalRequests);
        ctx.Cars.RemoveRange(ctx.Cars);

        var users = await ctx.Users
            .Include(u => u.Roles)
            .ToListAsync();
        ctx.Users.RemoveRange(users);

        await ctx.SaveChangesAsync();
    }
}
