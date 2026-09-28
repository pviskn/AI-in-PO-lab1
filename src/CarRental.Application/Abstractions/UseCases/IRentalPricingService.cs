using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions.UseCases;

public interface IRentalPricingService
{
    decimal CalculateBasePrice(Car car, DateOnly startDate, DateOnly endDate);

    decimal CalculateLateFee(Car car, DateOnly plannedReturnDate, DateOnly actualReturnDate);

    decimal CalculateTotalPrice(decimal basePrice, decimal lateFee, decimal damageFee);
}
