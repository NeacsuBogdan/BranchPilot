using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Customers;

public sealed class CustomerService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CustomerPageResponse> GetCustomersAsync(
        GetCustomersRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0
            ? Math.Min(request.PageSize, MaxPageSize)
            : DefaultPageSize;
        var normalizedSearch = request.Search?.Trim().ToUpperInvariant();

        var query = _dbContext.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(
                customer =>
                    customer.FirstName.ToUpper().Contains(normalizedSearch) ||
                    customer.LastName.ToUpper().Contains(normalizedSearch) ||
                    customer.Email.ToUpper().Contains(normalizedSearch) ||
                    customer.PhoneNumber.ToUpper().Contains(normalizedSearch));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(customer => customer.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var customers = await query
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = customers.Select(customer => customer.Id).ToArray();
        var bookingCounts = customerIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.Bookings
                .AsNoTracking()
                .Where(booking => customerIds.Contains(booking.CustomerId))
                .GroupBy(booking => booking.CustomerId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return new CustomerPageResponse(
            customers.Select(customer => ToCustomerResponse(customer, bookingCounts.GetValueOrDefault(customer.Id))).ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<CustomerResponse> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(existingCustomer => existingCustomer.Id == customerId, cancellationToken);

        if (customer is null)
        {
            throw new AppException(404, "Customer not found", "The selected customer could not be found.");
        }

        var bookingCount = await _dbContext.Bookings
            .AsNoTracking()
            .CountAsync(booking => booking.CustomerId == customer.Id, cancellationToken);

        return ToCustomerResponse(customer, bookingCount);
    }

    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var normalizedEmail = NormalizeEmail(request.Email);

        await EnsureEmailAvailableAsync(normalizedEmail, null, cancellationToken);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToCustomerResponse(customer, 0);
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var customer = await _dbContext.Customers
            .SingleOrDefaultAsync(existingCustomer => existingCustomer.Id == customerId, cancellationToken);

        if (customer is null)
        {
            throw new AppException(404, "Customer not found", "The selected customer could not be found.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        await EnsureEmailAvailableAsync(normalizedEmail, customer.Id, cancellationToken);

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = request.Email.Trim();
        customer.NormalizedEmail = normalizedEmail;
        customer.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        customer.Notes = request.Notes?.Trim() ?? string.Empty;
        customer.IsActive = request.IsActive;
        customer.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var bookingCount = await _dbContext.Bookings
            .AsNoTracking()
            .CountAsync(booking => booking.CustomerId == customer.Id, cancellationToken);

        return ToCustomerResponse(customer, bookingCount);
    }

    public async Task DeleteCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var customer = await _dbContext.Customers
            .SingleOrDefaultAsync(existingCustomer => existingCustomer.Id == customerId, cancellationToken);

        if (customer is null)
        {
            throw new AppException(404, "Customer not found", "The selected customer could not be found.");
        }

        var hasBookings = await _dbContext.Bookings
            .AnyAsync(booking => booking.CustomerId == customer.Id, cancellationToken);

        if (hasBookings)
        {
            throw new AppException(
                409,
                "Customer has bookings",
                "Customers with booking history cannot be deleted.");
        }

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid EnsureAuthenticatedTenant()
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.TenantId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }

        return _currentUserContext.TenantId.Value;
    }

    private async Task EnsureEmailAvailableAsync(
        string normalizedEmail,
        Guid? existingCustomerId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Customers
            .AnyAsync(
                customer =>
                    customer.NormalizedEmail == normalizedEmail &&
                    (!existingCustomerId.HasValue || customer.Id != existingCustomerId.Value),
                cancellationToken);

        if (exists)
        {
            throw new AppException(
                409,
                "Customer email already exists",
                "A customer with this email already exists in the current tenant.");
        }
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static CustomerResponse ToCustomerResponse(Customer customer, int bookingCount)
    {
        return new CustomerResponse(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            $"{customer.FirstName} {customer.LastName}".Trim(),
            customer.Email,
            customer.PhoneNumber,
            customer.Notes,
            customer.IsActive,
            bookingCount,
            customer.CreatedAtUtc);
    }
}
