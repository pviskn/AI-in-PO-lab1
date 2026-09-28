namespace CarRental.Domain.Entities;

public class RentalContract
{
    private RentalContract() { }

    public Guid Id { get; private set; }

    public Guid RentalRequestId { get; private set; }

    public RentalRequest RentalRequest { get; private set; } = null!;

    public decimal BasePrice { get; private set; }

    public DateOnly? ActualReturnDate { get; private set; }

    public decimal LateFee { get; private set; }

    public decimal DamageFee { get; private set; }

    public decimal TotalPrice { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public RentalContract(Guid id, Guid rentalRequestId, decimal basePrice)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id договора не может быть пустым", nameof(id));

        if (rentalRequestId == Guid.Empty)
            throw new ArgumentException("Id заявки не может быть пустым", nameof(rentalRequestId));

        if (basePrice < 0)
            throw new ArgumentException("стоимость не может быть отрицательной", nameof(basePrice));

        Id = id;
        RentalRequestId = rentalRequestId;
        BasePrice = basePrice;
        TotalPrice = basePrice;
        CreatedAt = DateTime.UtcNow;
    }

    public void Complete(DateOnly actualReturnDate, decimal lateFee, decimal damageFee)
    {
        if (ActualReturnDate.HasValue)
            throw new InvalidOperationException("договор уже был завершён");

        if (lateFee < 0)
            throw new ArgumentException("штраф за просрочку не может быть отрицательным", nameof(lateFee));

        if (damageFee < 0)
            throw new ArgumentException("штраф за повреждения не может быть отрицательным", nameof(damageFee));

        ActualReturnDate = actualReturnDate;
        LateFee = lateFee;
        DamageFee = damageFee;
        TotalPrice = BasePrice + lateFee + damageFee;
    }
}
