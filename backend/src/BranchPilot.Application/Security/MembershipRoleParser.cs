using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Security;

public static class MembershipRoleParser
{
    public static bool TryParse(string value, out MembershipRole role)
    {
        return Enum.TryParse(value, true, out role) && Enum.IsDefined(role);
    }
}
