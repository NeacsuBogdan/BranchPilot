using BranchPilot.Application.Locations;

namespace BranchPilot.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterOrganizationRequest(
    string TenantName,
    string PrimaryLocationName,
    string PrimaryLocationCode,
    string PrimaryLocationTimeZone,
    string FirstName,
    string LastName,
    string Email,
    string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthenticatedSessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    CurrentSessionResponse Session);

public sealed record CurrentSessionResponse(
    UserSummaryResponse User,
    MembershipSummaryResponse Membership,
    TenantSummaryResponse Tenant,
    IReadOnlyCollection<LocationResponse> Locations);

public sealed record UserSummaryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email);

public sealed record MembershipSummaryResponse(
    Guid Id,
    string Role,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<LocationResponse> AssignedLocations);

public sealed record TenantSummaryResponse(Guid Id, string Name, string Slug);
