using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.HasKey(c => c.Id);

        builder.OwnsOne(c => c.Vin, v =>
        {
            v.Property(p => p.Value)
             .HasColumnName("Vin")
             .HasMaxLength(17)
             .IsRequired();
        });

        builder.Property(c => c.Make)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Model)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Year)
            .IsRequired();

        builder.Property(c => c.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.DailyRate)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.Mileage)
            .IsRequired();

        builder.HasMany(c => c.RentalRequests)
               .WithOne(r => r.Car)
               .HasForeignKey(r => r.CarId);
    }
}
