namespace CarRental.Domain.Exceptions;

public sealed class CarNotAvailableException : DomainException
{
    public override int StatusCode => 409;

    public CarNotAvailableException(Guid carId)
        : base($"Автомобиль с идентификатором '{carId}' недоступен для аренды в указанный период.") { }
}
