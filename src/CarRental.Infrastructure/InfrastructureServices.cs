using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Services;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repositories;
using CarRental.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=car_rental;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRentalRequestRepository, RentalRequestRepository>();
        services.AddScoped<IRentalContractRepository, RentalContractRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICarCatalogService, CarCatalogService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRentalRequestService, RentalRequestService>();
        services.AddScoped<IRentalEligibilityService, RentalEligibilityService>();
        services.AddScoped<IRentalPricingService, RentalPricingService>();

        return services;
    }
}