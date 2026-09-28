using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using CarRental.Application.DTOs;
using CarRental.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarRental.IntegrationTests;

public class AuthControllerTests : TestBase
{
    public AuthControllerTests(DatabaseFixture fixture) : base(fixture) { }


    [Fact]
    public async Task Register_Returns201WithToken_WhenDataIsValid()
    {
        var dto = ValidRegisterDto("new_client");

        var response = await Client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.TokenType.Should().Be("Bearer");
        body.User.Username.Should().Be("new_client");
    }

    [Fact]
    public async Task Register_Returns409_WhenUsernameIsDuplicate()
    {
        var dto = ValidRegisterDto("dup_user");
        await Client.PostAsJsonAsync("/api/auth/register", dto);

        var response = await Client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_Returns400_WhenPasswordTooShort()
    {
        var dto = ValidRegisterDto("short_pwd") with { Password = "123" };

        var response = await Client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_AssignsClientRole_ToNewUser()
    {
        var dto = ValidRegisterDto("role_check");

        var response = await Client.PostAsJsonAsync("/api/auth/register", dto);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        body!.User.Roles.Should().Contain("Client");
    }


    [Fact]
    public async Task Login_Returns200WithToken_WhenCredentialsAreValid()
    {
        await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterDto("login_user"));

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("login_user", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordIsWrong()
    {
        await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterDto("wrong_pwd_user"));

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("wrong_pwd_user", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Returns401_WhenUserDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("nonexistent_user", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_TokenContainsCorrectClaims()
    {
        var dto = ValidRegisterDto("claims_user");
        await Client.PostAsJsonAsync("/api/auth/register", dto);

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("claims_user", "Password123!"));
        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body!.AccessToken);

        jwt.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "claims_user");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Client");
    }


    private static RegisterDto ValidRegisterDto(string username)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new RegisterDto(username, "Password123!", "Test", "User",
            $"{username}@test.com", today.AddYears(-25), today.AddYears(-5));
    }
}
