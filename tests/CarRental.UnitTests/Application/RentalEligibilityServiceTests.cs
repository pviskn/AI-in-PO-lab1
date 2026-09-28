using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CarRental.UnitTests.Application;

public class RentalEligibilityServiceTests
{
    private readonly RentalEligibilityService _service = new();

    [Theory]
    [InlineData(18, 1, CarCategory.Economy, true)]
    [InlineData(18, 1, CarCategory.Standard, false)]
    [InlineData(21, 2, CarCategory.Standard, true)]
    [InlineData(20, 2, CarCategory.Standard, false)]
    [InlineData(24, 5, CarCategory.Premium, false)]
    [InlineData(25, 3, CarCategory.Premium, true)]
    [InlineData(25, 5, CarCategory.Sport, true)]
    [InlineData(25, 4, CarCategory.Sport, false)]
    [InlineData(24, 5, CarCategory.Sport, false)]
    public void EnsureEligible_ShouldRespectCategoryRequirements(
        int ageYears,
        int experienceYears,
        CarCategory category,
        bool shouldBeEligible)
    {
        User user = CreateUser(ageYears, experienceYears);
        Action act = () => _service.EnsureEligible(user, category);

        if (shouldBeEligible)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<DomainException>();
        }
    }

    [Fact]
    public void EnsureEligible_ShouldThrowUserNotEligible_WhenAgeTooLow()
    {
        User user = CreateUser(17, 3);

        Action act = () => _service.EnsureEligible(user, CarCategory.Economy);

        act.Should().ThrowExactly<UserNotEligibleException>();
    }

    [Fact]
    public void EnsureEligible_ShouldThrowInsufficientExperience_WhenExperienceTooLow()
    {
        User user = CreateUser(25, 0);

        Action act = () => _service.EnsureEligible(user, CarCategory.Sport);

        act.Should().ThrowExactly<InsufficientDriverExperienceException>();
    }

    [Fact]
    public void EnsureEligible_ShouldNotThrow_ForEconomy_WithMinimumRequirements()
    {
        User user = CreateUser(18, 1);

        Action act = () => _service.EnsureEligible(user, CarCategory.Economy);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureEligible_ShouldThrow_ForPremium_WhenAge24AndExperience5()
    {
        User user = CreateUser(24, 5);

        Action act = () => _service.EnsureEligible(user, CarCategory.Premium);

        act.Should().Throw<UserNotEligibleException>();
    }

    private static User CreateUser(int ageYears, int experienceYears)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly dateOfBirth = today.AddYears(-ageYears);
        DateOnly licenseDate = today.AddYears(-experienceYears);

        return new User(
            Guid.NewGuid(),
            "testuser",
            "hash",
            "Test",
            "User",
            dateOfBirth,
            licenseDate,
            "test@example.com");
    }
}