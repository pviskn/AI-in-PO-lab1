using CarRental.Domain.Enums;

namespace CarRental.Domain.Exceptions;

public sealed class InsufficientDriverExperienceException : DomainException
{
    public override int StatusCode => 400;

    public InsufficientDriverExperienceException(CarCategory category, int requiredYears, int actualYears)
        : base($"Для аренды автомобиля категории '{category}' требуется стаж не менее {requiredYears} лет. " +
               $"Фактический стаж: {actualYears} лет.") { }
}
