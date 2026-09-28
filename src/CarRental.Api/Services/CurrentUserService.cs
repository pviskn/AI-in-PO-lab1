using CarRental.Application.Abstractions.Services;
using System.Security.Claims;

namespace CarRental.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            string? userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out Guid userId))
                return null;

            return userId;
        }
    }

    public string? Username =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public IReadOnlyList<string> Roles
    {
        get
        {
            var claims = _httpContextAccessor.HttpContext?.User
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return claims ?? new List<string>();
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}