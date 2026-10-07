using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Domain.ValueObjects;

namespace CarRental.Domain.Entities;

public class Car
{
    private Car() { }

    public Guid Id { get; private set; }

    public Vin Vin { get; private set; } = null!;

    public string Make { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public CarCategory Category { get; private set; }

    public CarStatus Status { get; private set; }

    public decimal DailyRate { get; private set; }

    public int Mileage { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    private readonly List<RentalRequest> _rentalRequests = new();

    public IReadOnlyCollection<RentalRequest> RentalRequests => _rentalRequests.AsReadOnly();

    public Car(Guid id, string vin, string make, string model, int year, CarCategory category, decimal dailyRate, int mileage)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id автомобиля не может быть пустым", nameof(id));

        if (string.IsNullOrWhiteSpace(make))
            throw new ArgumentException("марка автомобиля не может быть пустой", nameof(make));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("модель автомобиля не может быть пустой", nameof(model));

        if (year < 1900 || year > 2100)
            throw new ArgumentException("год выпуска указан некорректно", nameof(year));

        if (dailyRate <= 0)
            throw new ArgumentException("стоимость аренды должна быть больше нуля", nameof(dailyRate));

        if (mileage < 0)
            throw new ArgumentException("пробег не может быть отрицательным", nameof(mileage));

        Id = id;
        Vin = Vin.Create(vin);
        Make = make.Trim();
        Model = model.Trim();
        Year = year;
        Category = category;
        DailyRate = dailyRate;
        Mileage = mileage;

        Status = CarStatus.Available;
    }

    public void Rent()
    {
        if (IsDeleted || Status != CarStatus.Available)
            throw new CarNotAvailableException(Id);

        Status = CarStatus.Rented;
    }

    public void Complete()
    {
        if (Status != CarStatus.Rented)
            throw new InvalidOperationException("можно завершить только rented");

        Status = CarStatus.Available;
    }

    public void SendToMaintenance()
    {
        if (Status != CarStatus.Available)
            throw new InvalidOperationException("на обслуживание можно отправить только available");

        Status = CarStatus.UnderMaintenance;
    }

    public void ReturnFromMaintenance()
    {
        if (Status != CarStatus.UnderMaintenance)
            throw new InvalidOperationException("автомобиль не на обслуживании");

        Status = CarStatus.Available;
    }

    public void MarkAsDeleted(DateTime deletedAt)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Автомобиль уже удалён");

        DeletedAt = deletedAt;
    }

    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("Автомобиль не удалён");

        DeletedAt = null;
    }
}
