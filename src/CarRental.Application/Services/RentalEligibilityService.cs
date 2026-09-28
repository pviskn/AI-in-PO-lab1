using CarRental.Application.Abstractions.UseCases;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;

namespace CarRental.Application.Services;

public class RentalEligibilityService : IRentalEligibilityService
{
    public void EnsureEligible(User user, CarCategory category)
    {
        (int minAge, int minStazh) = category switch
        {
            CarCategory.Economy => (18, 1),
            CarCategory.Standard => (21, 2),
            CarCategory.Premium => (25, 3),
            CarCategory.Sport => (25, 5),
            _ => (21, 2),
        };

        if (user.AgeInYears < minAge)
            throw new UserNotEligibleException(user.Id, $"для категории {category} нужен возраст не менее {minAge} лет");

        if (user.DrivingExperienceYears < minStazh)
            throw new InsufficientDriverExperienceException(category, minStazh, user.DrivingExperienceYears);
    }
}
