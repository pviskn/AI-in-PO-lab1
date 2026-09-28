using CarRental.Application.Abstractions.UseCases;
using CarRental.Domain.Entities;

namespace CarRental.Application.Services;

public class RentalPricingService : IRentalPricingService
{
    private const decimal LateMultiplier = 1.5m;

    public decimal CalculateBasePrice(Car car, DateOnly startDate, DateOnly endDate)
    {
        int dni = endDate.DayNumber - startDate.DayNumber;
        return car.DailyRate * dni;
    }

    public decimal CalculateLateFee(Car car, DateOnly plannedReturnDate, DateOnly actualReturnDate)
    {
        int dniProsrochki = actualReturnDate.DayNumber - plannedReturnDate.DayNumber;
        if (dniProsrochki <= 0)
            return 0m;

        return car.DailyRate * LateMultiplier * dniProsrochki;
    }

    public decimal CalculateTotalPrice(decimal basePrice, decimal lateFee, decimal damageFee)
    {
        return basePrice + lateFee + damageFee;
    }
}
