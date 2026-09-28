using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(role => role.Id);

        builder.HasIndex(role => role.Name)
            .IsUnique();

        builder.Property(role => role.Name)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
    }
}
