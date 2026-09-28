using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class RentalContractConfiguration : IEntityTypeConfiguration<RentalContract>
{
    public void Configure(EntityTypeBuilder<RentalContract> builder)
    {
        builder.HasKey(contract => contract.Id);

        // один-к-одному заявка - договор
        builder.HasOne(contract => contract.RentalRequest)
               .WithOne(request => request.Contract)
               .HasForeignKey<RentalContract>(contract => contract.RentalRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(contract => contract.BasePrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(contract => contract.LateFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(contract => contract.DamageFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(contract => contract.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(contract => contract.CreatedAt)
            .IsRequired();

        builder.Property(contract => contract.ActualReturnDate)
            .IsRequired(false);
    }
}
