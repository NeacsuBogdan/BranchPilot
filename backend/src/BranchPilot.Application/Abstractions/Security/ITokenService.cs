using BranchPilot.Domain.Entities;

namespace BranchPilot.Application.Abstractions.Security;

public interface ITokenService
{
    TokenPair CreateTokenPair(AppUser user, Tenant tenant, DateTimeOffset issuedAtUtc);

    string HashRefreshToken(string refreshToken);
}

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
