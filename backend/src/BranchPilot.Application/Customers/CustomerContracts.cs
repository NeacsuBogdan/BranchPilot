using BranchPilot.Application.Common;

namespace BranchPilot.Application.Customers;

public sealed record GetCustomersRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);

public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Notes);

public sealed record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Notes,
    bool IsActive);

public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    string Notes,
    bool IsActive,
    int BookingCount,
    DateTimeOffset CreatedAtUtc);

public sealed record CustomerPageResponse(
    IReadOnlyCollection<CustomerResponse> Items,
    int Page,
    int PageSize,
    int TotalCount) : PagedResponse<CustomerResponse>(Items, Page, PageSize, TotalCount);
