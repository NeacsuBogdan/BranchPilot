using BranchPilot.Domain.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.Domain.Entities;

public sealed class Membership : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public MembershipRole Role { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
