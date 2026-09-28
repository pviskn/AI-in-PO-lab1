using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions.UseCases;

public interface IRentalEligibilityService
{
    void EnsureEligible(User user, CarCategory category);
}
