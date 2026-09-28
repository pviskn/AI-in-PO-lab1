using System.Net;
using System.Net.Http.Json;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;
using CarRental.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarRental.IntegrationTests;

public class UsersControllerTests : TestBase
{
    public UsersControllerTests(DatabaseFixture fixture) : base(fixture) { }


    [Fact]
    public async Task CreateUser_Returns401_WhenNotAuthenticated()
    {
        var response = await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("anon_create"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_Returns403_WhenCalledByClient()
    {
        var clientToken = await RegisterAndGetTokenAsync("client_user_mgmt");
        SetAuthToken(clientToken);

        var response = await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("blocked_user"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_Returns403_WhenCalledByManager()
    {
        var managerToken = await CreateManagerAndGetTokenAsync("mgr_user_mgmt");
        SetAuthToken(managerToken);

        var response = await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("blocked_user2"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_Returns201_WhenAdminCreatesValidUser()
    {
        var adminToken = await CreateAdminAndGetTokenAsync();
        SetAuthToken(adminToken);

        var response = await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("admin_created"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body!.Username.Should().Be("admin_created");
        body.Roles.Should().Contain("Client");
    }

    [Fact]
    public async Task CreateUser_Returns409_WhenUsernameIsDuplicate()
    {
        var adminToken = await CreateAdminAndGetTokenAsync("admin_dup");
        SetAuthToken(adminToken);
        await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("dup_username"));

        var response = await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("dup_username"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }


    [Fact]
    public async Task AssignRole_Returns204_WhenAdminAssignsManagerRole()
    {
        var adminToken = await CreateAdminAndGetTokenAsync("admin_assign");
        SetAuthToken(adminToken);

        var created = await (await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("to_be_manager")))
            .Content.ReadFromJsonAsync<UserDto>();

        var response = await Client.PostAsJsonAsync(
            $"/api/users/{created!.Id}/roles",
            new AssignRoleDto(UserRole.Manager));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignRole_Returns400_WhenRoleAlreadyAssigned()
    {
        var adminToken = await CreateAdminAndGetTokenAsync("admin_dup_role");
        SetAuthToken(adminToken);

        var created = await (await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("already_has_role")))
            .Content.ReadFromJsonAsync<UserDto>();

        var response = await Client.PostAsJsonAsync(
            $"/api/users/{created!.Id}/roles",
            new AssignRoleDto(UserRole.Client));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignRole_Returns404_WhenUserDoesNotExist()
    {
        var adminToken = await CreateAdminAndGetTokenAsync("admin_nouser");
        SetAuthToken(adminToken);

        var response = await Client.PostAsJsonAsync(
            $"/api/users/{Guid.NewGuid()}/roles",
            new AssignRoleDto(UserRole.Manager));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task GetUsers_Returns200WithList_WhenCalledByAdmin()
    {
        var adminToken = await CreateAdminAndGetTokenAsync("admin_list");
        SetAuthToken(adminToken);
        await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("list_user_1"));
        await Client.PostAsJsonAsync("/api/users", ValidCreateUserDto("list_user_2"));

        var response = await Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
        body!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetUsers_Returns403_WhenCalledByClient()
    {
        var clientToken = await RegisterAndGetTokenAsync("client_get_users");
        SetAuthToken(clientToken);

        var response = await Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    private static CreateUserDto ValidCreateUserDto(string username)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new CreateUserDto(username, "Password123!", "Test", "User",
            $"{username}@test.com", today.AddYears(-25), today.AddYears(-5));
    }
}
