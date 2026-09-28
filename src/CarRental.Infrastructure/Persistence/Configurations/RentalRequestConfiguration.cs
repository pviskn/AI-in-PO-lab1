using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class RentalRequestConfiguration : IEntityTypeConfiguration<RentalRequest>
{
    public void Configure(EntityTypeBuilder<RentalRequest> builder)
    {
        builder.HasKey(r => r.Id);

        // клиент
        builder.HasOne(r => r.User)
               .WithMany(u => u.RentalRequests)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // автомобиль
        builder.HasOne(r => r.Car)
               .WithMany(c => c.RentalRequests)
               .HasForeignKey(r => r.CarId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Status)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(r => r.ManagerComment)
               .HasMaxLength(500);

        builder.Property(r => r.StartDate).IsRequired();
        builder.Property(r => r.EndDate).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
    }
}
