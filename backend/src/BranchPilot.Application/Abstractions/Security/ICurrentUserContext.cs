namespace BranchPilot.Application.Abstractions.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? TenantId { get; }

    string? Email { get; }
}
