using System.Security.Claims;
using BranchPilot.Application.Abstractions.Security;
using Microsoft.AspNetCore.Http;

namespace BranchPilot.Infrastructure.Auth;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ParseGuid(
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
        Principal?.FindFirstValue("sub"));

    public Guid? TenantId => ParseGuid(Principal?.FindFirstValue("tenant_id"));

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email) ??
        Principal?.FindFirstValue("email");

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsedValue) ? parsedValue : null;
    }
}
