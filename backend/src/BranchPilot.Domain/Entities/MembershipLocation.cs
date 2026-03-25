using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class MembershipLocation : ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid MembershipId { get; set; }

    public Guid LocationId { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; }
}
