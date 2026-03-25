using BranchPilot.Domain.Entities;

namespace BranchPilot.Application.Abstractions.Security;

public interface IPasswordService
{
    string HashPassword(AppUser user, string password);

    bool VerifyPassword(AppUser user, string password);
}
