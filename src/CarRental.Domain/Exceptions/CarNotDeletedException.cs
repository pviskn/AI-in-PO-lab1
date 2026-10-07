namespace CarRental.Domain.Exceptions;

public sealed class CarNotDeletedException : DomainException
{
    public override int StatusCode => 409;

    public CarNotDeletedException(Guid carId)
        : base($"Автомобиль с идентификатором '{carId}' не удалён, восстановление невозможно.") { }
}
