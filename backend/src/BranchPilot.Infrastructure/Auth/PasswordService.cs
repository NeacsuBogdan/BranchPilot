using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BranchPilot.Infrastructure.Auth;

public sealed class PasswordService : IPasswordService
{
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public PasswordService(IPasswordHasher<AppUser> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string HashPassword(AppUser user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(AppUser user, string password)
    {
        return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
    }
}
