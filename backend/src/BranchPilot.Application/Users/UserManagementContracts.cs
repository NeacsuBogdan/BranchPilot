using BranchPilot.Application.Common;

namespace BranchPilot.Application.Users;

public sealed record GetUsersRequest(int Page = 1, int PageSize = 10, string? Search = null);

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role,
    IReadOnlyCollection<Guid> LocationIds);

public sealed record UpdateUserMembershipRequest(
    string Role,
    bool IsActive,
    IReadOnlyCollection<Guid> LocationIds);

public sealed record TeamMemberResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    bool IsActive,
    bool IsCurrentUser,
    MembershipDetailsResponse Membership);

public sealed record MembershipDetailsResponse(
    Guid Id,
    string Role,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<LocationAssignmentResponse> AssignedLocations);

public sealed record LocationAssignmentResponse(Guid Id, string Name, string Code);

public sealed record RoleOptionResponse(
    string Code,
    string Name,
    string Description,
    IReadOnlyCollection<string> Permissions);

public sealed record TeamMemberPageResponse(
    IReadOnlyCollection<TeamMemberResponse> Items,
    int Page,
    int PageSize,
    int TotalCount) : PagedResponse<TeamMemberResponse>(Items, Page, PageSize, TotalCount);
