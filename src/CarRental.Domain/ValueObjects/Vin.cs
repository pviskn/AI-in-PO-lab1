namespace CarRental.Domain.ValueObjects;

public sealed class Vin : IEquatable<Vin>
{
    public string Value { get; private set; } = string.Empty;

    private Vin() { }

    private Vin(string value)
    {
        Value = value;
    }

    public static Vin Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("VIN не может быть пустым", nameof(value));

        string vin = value.Trim().ToUpperInvariant();
        if (vin.Length != 17)
            throw new ArgumentException("VIN должен содержать 17 знаков", nameof(value));

        foreach (char bukva in vin)
        {
            bool etoBukva = bukva is >= 'A' and <= 'Z';
            bool etoCifra = bukva is >= '0' and <= '9';

            if (!etoBukva && !etoCifra)
                throw new ArgumentException("VIN с недопустимыми символами", nameof(value));

            if (bukva is 'I' or 'O' or 'Q')
                throw new ArgumentException("VIN не должен содержать I, O или Q", nameof(value));
        }

        return new Vin(vin);
    }

    public bool Equals(Vin? other)
    {
        if (other is null)
            return false;

        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vin other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return Value;
    }
}
