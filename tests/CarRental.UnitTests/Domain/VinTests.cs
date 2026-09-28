using CarRental.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CarRental.UnitTests.Domain;

public class VinTests
{
    [Theory]
    [InlineData("1HGCM82633A004352")]
    [InlineData("JH4KA7650MC002594")]
    [InlineData("WVWZZZ1JZ3W386752")]
    public void Create_ShouldSucceed_WhenVinIsValid(string vin)
    {
        var result = Vin.Create(vin);

        result.Value.Should().Be(vin.ToUpperInvariant());
    }

    [Fact]
    public void Create_ShouldNormalizeLowercase()
    {
        var vin = Vin.Create("1hgcm82633a004352");

        vin.Value.Should().Be("1HGCM82633A004352");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TOOSHORT")]
    [InlineData("1HGCM82633A004352X")]
    [InlineData("1HGCM82633I004352")]
    [InlineData("1HGCM82633O004352")]
    [InlineData("1HGCM82633Q004352")]
    [InlineData("1HGCM826 3A004352")]
    [InlineData("1HGCM82633A0043#2")]
    public void Create_ShouldThrow_WhenVinIsInvalid(string invalidVin)
    {
        Action act = () => Vin.Create(invalidVin);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_ShouldBeTrue_ForSameVin()
    {
        var vin1 = Vin.Create("1HGCM82633A004352");
        var vin2 = Vin.Create("1HGCM82633A004352");

        vin1.Should().Be(vin2);
    }

    [Fact]
    public void Equals_ShouldBeFalse_ForDifferentVin()
    {
        var vin1 = Vin.Create("1HGCM82633A004352");
        var vin2 = Vin.Create("JH4KA7650MC002594");

        vin1.Should().NotBe(vin2);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var vin = Vin.Create("1HGCM82633A004352");

        vin.ToString().Should().Be("1HGCM82633A004352");
    }
}
