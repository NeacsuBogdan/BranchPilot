using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Entities;
using BranchPilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Bookings;

public sealed class BookingService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookingService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BookingOptionsResponse> GetBookingOptionsAsync(CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var customersTask = _dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.IsActive)
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .Select(
                customer => new BookingCustomerOptionResponse(
                    customer.Id,
                    $"{customer.FirstName} {customer.LastName}".Trim(),
                    customer.Email))
            .ToListAsync(cancellationToken);

        var locationsTask = _dbContext.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .Select(location => new BookingLocationOptionResponse(location.Id, location.Name, location.Code, location.TimeZone))
            .ToListAsync(cancellationToken);

        var servicesTask = _dbContext.CatalogItems
            .AsNoTracking()
            .Where(
                item =>
                    item.IsActive &&
                    item.ItemType == CatalogItemType.Service &&
                    item.DurationInMinutes.HasValue)
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.Code, item.DurationInMinutes })
            .ToListAsync(cancellationToken);

        await Task.WhenAll(customersTask, locationsTask, servicesTask);

        var serviceIds = servicesTask.Result.Select(service => service.Id).ToArray();
        var prices = serviceIds.Length == 0
            ? []
            : await _dbContext.LocationPrices
                .AsNoTracking()
                .Where(locationPrice => serviceIds.Contains(locationPrice.CatalogItemId))
                .OrderBy(locationPrice => locationPrice.LocationId)
                .Select(
                    locationPrice =>
                        new
                        {
                            locationPrice.CatalogItemId,
                            Response = new BookingServicePriceOptionResponse(
                                locationPrice.LocationId,
                                locationPrice.PriceAmount,
                                locationPrice.CurrencyCode),
                        })
                .ToListAsync(cancellationToken);

        var pricesByService = prices
            .GroupBy(item => item.CatalogItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<BookingServicePriceOptionResponse>)group.Select(item => item.Response).ToArray());

        var services = servicesTask.Result
            .Where(service => pricesByService.ContainsKey(service.Id))
            .Select(
                service => new BookingServiceOptionResponse(
                    service.Id,
                    service.Name,
                    service.Code,
                    service.DurationInMinutes ?? 0,
                    pricesByService[service.Id]))
            .ToArray();

        return new BookingOptionsResponse(customersTask.Result, locationsTask.Result, services);
    }

    public async Task<BookingPageResponse> GetBookingsAsync(
        GetBookingsRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0
            ? Math.Min(request.PageSize, MaxPageSize)
            : DefaultPageSize;
        var normalizedSearch = request.Search?.Trim().ToUpperInvariant();
        BookingStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            statusFilter = ParseStatus(request.Status);
        }

        var query = _dbContext.Bookings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(
                booking =>
                    booking.Number.ToUpper().Contains(normalizedSearch) ||
                    _dbContext.Customers.Any(
                        customer =>
                            customer.Id == booking.CustomerId &&
                            (
                                customer.FirstName.ToUpper().Contains(normalizedSearch) ||
                                customer.LastName.ToUpper().Contains(normalizedSearch) ||
                                customer.Email.ToUpper().Contains(normalizedSearch))));
        }

        if (request.LocationId.HasValue)
        {
            query = query.Where(booking => booking.LocationId == request.LocationId.Value);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(booking => booking.Status == statusFilter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var bookings = await query
            .OrderBy(booking => booking.StartsAtUtc)
            .ThenBy(booking => booking.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(booking => booking.Id).ToArray();
        var customerIds = bookings.Select(booking => booking.CustomerId).Distinct().ToArray();
        var locationIds = bookings.Select(booking => booking.LocationId).Distinct().ToArray();

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(
                customer => customer.Id,
                customer => new BookingCustomerSummaryResponse(
                    customer.Id,
                    $"{customer.FirstName} {customer.LastName}".Trim(),
                    customer.Email),
                cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(location => locationIds.Contains(location.Id))
            .ToDictionaryAsync(
                location => location.Id,
                location => new BookingLocationSummaryResponse(location.Id, location.Name, location.Code),
                cancellationToken);

        var lineCounts = bookingIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.BookingLines
                .AsNoTracking()
                .Where(line => bookingIds.Contains(line.BookingId))
                .GroupBy(line => line.BookingId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var items = bookings
            .Select(
                booking => new BookingListItemResponse(
                    booking.Id,
                    booking.Number,
                    booking.Status.ToString(),
                    booking.StartsAtUtc,
                    booking.EndsAtUtc,
                    booking.Notes,
                    customers[booking.CustomerId],
                    locations[booking.LocationId],
                    booking.TotalAmount,
                    booking.CurrencyCode,
                    booking.TotalDurationInMinutes,
                    lineCounts.GetValueOrDefault(booking.Id)))
            .ToArray();

        return new BookingPageResponse(items, page, pageSize, totalCount);
    }

    public async Task<BookingDetailsResponse> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var booking = await GetBookingEntityAsync(bookingId, cancellationToken);
        return await BuildBookingDetailsAsync(booking, cancellationToken);
    }

    public async Task<BookingDetailsResponse> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var now = _dateTimeProvider.UtcNow;

        BookingBusinessRules.EnsureHasLines(request.Lines);
        BookingBusinessRules.EnsureDistinctCatalogItems(request.Lines);
        BookingBusinessRules.EnsureStartsInFuture(request.StartsAtUtc, now);

        var preparedBooking = await PrepareBookingAsync(
            request.CustomerId,
            request.LocationId,
            request.StartsAtUtc,
            request.Lines,
            cancellationToken);

        await EnsureNoOverlappingBookingsAsync(
            request.LocationId,
            request.StartsAtUtc,
            preparedBooking.EndsAtUtc,
            null,
            cancellationToken);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = request.LocationId,
            CustomerId = request.CustomerId,
            Number = GenerateBookingNumber(now),
            Status = BookingStatus.Scheduled,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = preparedBooking.EndsAtUtc,
            Notes = request.Notes?.Trim() ?? string.Empty,
            CurrencyCode = preparedBooking.CurrencyCode,
            TotalAmount = preparedBooking.TotalAmount,
            TotalDurationInMinutes = preparedBooking.TotalDurationInMinutes,
            CreatedAtUtc = now,
        };

        var lines = preparedBooking.Lines
            .Select(
                line => new BookingLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    BookingId = booking.Id,
                    CatalogItemId = line.CatalogItemId,
                    ItemName = line.ItemName,
                    Quantity = line.Quantity,
                    DurationInMinutes = line.DurationInMinutes,
                    UnitPriceAmount = line.UnitPriceAmount,
                    LineTotalAmount = line.LineTotalAmount,
                })
            .ToArray();

        await _dbContext.Bookings.AddAsync(booking, cancellationToken);
        await _dbContext.BookingLines.AddRangeAsync(lines, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildBookingDetailsAsync(booking, cancellationToken);
    }

    public async Task<BookingDetailsResponse> ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var booking = await GetBookingEntityAsync(bookingId, cancellationToken);
        BookingBusinessRules.EnsureCanConfirm(booking.Status);

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await BuildBookingDetailsAsync(booking, cancellationToken);
    }

    public async Task<BookingDetailsResponse> CompleteBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var booking = await GetBookingEntityAsync(bookingId, cancellationToken);
        BookingBusinessRules.EnsureCanComplete(booking.Status);

        booking.Status = BookingStatus.Completed;
        booking.CompletedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await BuildBookingDetailsAsync(booking, cancellationToken);
    }

    public async Task<BookingDetailsResponse> RescheduleBookingAsync(
        Guid bookingId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var booking = await GetBookingEntityAsync(bookingId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        BookingBusinessRules.EnsureCanReschedule(booking.Status);
        BookingBusinessRules.EnsureStartsInFuture(request.StartsAtUtc, now);

        var newEndsAtUtc = request.StartsAtUtc.AddMinutes(booking.TotalDurationInMinutes);
        await EnsureNoOverlappingBookingsAsync(
            booking.LocationId,
            request.StartsAtUtc,
            newEndsAtUtc,
            booking.Id,
            cancellationToken);

        booking.StartsAtUtc = request.StartsAtUtc;
        booking.EndsAtUtc = newEndsAtUtc;
        booking.RescheduledAtUtc = now;
        booking.RescheduleReason = request.Reason?.Trim() ?? string.Empty;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await BuildBookingDetailsAsync(booking, cancellationToken);
    }

    public async Task<BookingDetailsResponse> CancelBookingAsync(
        Guid bookingId,
        CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var booking = await GetBookingEntityAsync(bookingId, cancellationToken);
        BookingBusinessRules.EnsureCanCancel(booking.Status);

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = _dateTimeProvider.UtcNow;
        booking.CancellationReason = request.Reason.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await BuildBookingDetailsAsync(booking, cancellationToken);
    }

    private Guid EnsureAuthenticatedTenant()
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.TenantId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }

        return _currentUserContext.TenantId.Value;
    }

    private async Task<PreparedBookingData> PrepareBookingAsync(
        Guid customerId,
        Guid locationId,
        DateTimeOffset startsAtUtc,
        IReadOnlyCollection<CreateBookingLineRequest> lineRequests,
        CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(existingCustomer => existingCustomer.Id == customerId, cancellationToken);

        if (customer is null || !customer.IsActive)
        {
            throw new AppException(
                400,
                "Customer not available",
                "The selected customer could not be found or is inactive.");
        }

        var location = await _dbContext.Locations
            .AsNoTracking()
            .SingleOrDefaultAsync(existingLocation => existingLocation.Id == locationId, cancellationToken);

        if (location is null)
        {
            throw new AppException(400, "Location not found", "The selected location could not be found.");
        }

        var catalogItemIds = lineRequests.Select(line => line.CatalogItemId).Distinct().ToArray();
        var catalogItems = await _dbContext.CatalogItems
            .AsNoTracking()
            .Where(item => catalogItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (catalogItems.Count != catalogItemIds.Length)
        {
            throw new AppException(
                400,
                "Service not found",
                "One or more selected services could not be found in the current tenant.");
        }

        if (catalogItems.Any(
                item =>
                    !item.IsActive ||
                    item.ItemType != CatalogItemType.Service ||
                    !item.DurationInMinutes.HasValue))
        {
            throw new AppException(
                409,
                "Unsupported booking service",
                "Only active service catalog items can be scheduled as bookings.");
        }

        var prices = await _dbContext.LocationPrices
            .AsNoTracking()
            .Where(
                locationPrice =>
                    locationPrice.LocationId == locationId &&
                    catalogItemIds.Contains(locationPrice.CatalogItemId))
            .ToListAsync(cancellationToken);

        if (prices.Count != catalogItemIds.Length)
        {
            throw new AppException(
                409,
                "Missing location pricing",
                "Each selected service must have a price configured for the chosen location.");
        }

        BookingBusinessRules.EnsureSingleCurrency(prices.Select(price => price.CurrencyCode).ToArray());

        var itemsById = catalogItems.ToDictionary(item => item.Id);
        var pricesByItemId = prices.ToDictionary(price => price.CatalogItemId);
        var lines = lineRequests
            .Select(
                line =>
                {
                    var item = itemsById[line.CatalogItemId];
                    var price = pricesByItemId[line.CatalogItemId];
                    return new PreparedBookingLine(
                        line.CatalogItemId,
                        item.Name,
                        line.Quantity,
                        item.DurationInMinutes!.Value * line.Quantity,
                        price.PriceAmount,
                        price.PriceAmount * line.Quantity);
                })
            .ToArray();

        var totalDurationInMinutes = lines.Sum(line => line.DurationInMinutes);
        var totalAmount = lines.Sum(line => line.LineTotalAmount);

        return new PreparedBookingData(
            customer.Id,
            location.Id,
            startsAtUtc.AddMinutes(totalDurationInMinutes),
            totalDurationInMinutes,
            totalAmount,
            prices[0].CurrencyCode,
            lines);
    }

    private async Task EnsureNoOverlappingBookingsAsync(
        Guid locationId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? existingBookingId,
        CancellationToken cancellationToken)
    {
        var overlapExists = await _dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(
                booking =>
                    booking.LocationId == locationId &&
                    (booking.Status == BookingStatus.Scheduled || booking.Status == BookingStatus.Confirmed) &&
                    (!existingBookingId.HasValue || booking.Id != existingBookingId.Value) &&
                    booking.StartsAtUtc < endsAtUtc &&
                    startsAtUtc < booking.EndsAtUtc,
                cancellationToken);

        if (overlapExists)
        {
            throw new AppException(
                409,
                "Booking overlap detected",
                "Another booking is already scheduled for this location during the selected time window.");
        }
    }

    private async Task<Booking> GetBookingEntityAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .SingleOrDefaultAsync(existingBooking => existingBooking.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            throw new AppException(404, "Booking not found", "The selected booking could not be found.");
        }

        return booking;
    }

    private async Task<BookingDetailsResponse> BuildBookingDetailsAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(existingCustomer => existingCustomer.Id == booking.CustomerId)
            .Select(
                existingCustomer => new BookingCustomerSummaryResponse(
                    existingCustomer.Id,
                    $"{existingCustomer.FirstName} {existingCustomer.LastName}".Trim(),
                    existingCustomer.Email))
            .SingleAsync(cancellationToken);

        var location = await _dbContext.Locations
            .AsNoTracking()
            .Where(existingLocation => existingLocation.Id == booking.LocationId)
            .Select(existingLocation => new BookingLocationSummaryResponse(existingLocation.Id, existingLocation.Name, existingLocation.Code))
            .SingleAsync(cancellationToken);

        var lines = await _dbContext.BookingLines
            .AsNoTracking()
            .Where(line => line.BookingId == booking.Id)
            .OrderBy(line => line.ItemName)
            .Select(
                line => new BookingLineResponse(
                    line.Id,
                    line.CatalogItemId,
                    line.ItemName,
                    line.Quantity,
                    line.DurationInMinutes,
                    line.UnitPriceAmount,
                    line.LineTotalAmount))
            .ToListAsync(cancellationToken);

        return new BookingDetailsResponse(
            booking.Id,
            booking.Number,
            booking.Status.ToString(),
            booking.StartsAtUtc,
            booking.EndsAtUtc,
            booking.Notes,
            customer,
            location,
            booking.TotalAmount,
            booking.CurrencyCode,
            booking.TotalDurationInMinutes,
            booking.CancellationReason,
            booking.RescheduleReason,
            lines,
            BuildTimeline(booking));
    }

    private static IReadOnlyCollection<BookingTimelineEntryResponse> BuildTimeline(Booking booking)
    {
        var timeline = new List<BookingTimelineEntryResponse>
        {
            new("Created", booking.CreatedAtUtc, "Booking created and scheduled."),
        };

        if (booking.ConfirmedAtUtc.HasValue)
        {
            timeline.Add(new("Confirmed", booking.ConfirmedAtUtc.Value, "Booking confirmed for operations."));
        }

        if (booking.RescheduledAtUtc.HasValue)
        {
            timeline.Add(
                new(
                    "Rescheduled",
                    booking.RescheduledAtUtc.Value,
                    string.IsNullOrWhiteSpace(booking.RescheduleReason)
                        ? "Booking schedule updated."
                        : $"Booking schedule updated. {booking.RescheduleReason}"));
        }

        if (booking.CompletedAtUtc.HasValue)
        {
            timeline.Add(new("Completed", booking.CompletedAtUtc.Value, "Booking completed."));
        }

        if (booking.CancelledAtUtc.HasValue)
        {
            timeline.Add(
                new(
                    "Cancelled",
                    booking.CancelledAtUtc.Value,
                    string.IsNullOrWhiteSpace(booking.CancellationReason)
                        ? "Booking cancelled."
                        : $"Booking cancelled. {booking.CancellationReason}"));
        }

        return timeline
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ToArray();
    }

    private static BookingStatus ParseStatus(string value)
    {
        if (!BookingStatusParser.TryParse(value, out var status))
        {
            throw new AppException(400, "Unsupported booking status", "The selected booking status is not supported.");
        }

        return status;
    }

    private static string GenerateBookingNumber(DateTimeOffset now)
    {
        return $"BK-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
    }

    private sealed record PreparedBookingLine(
        Guid CatalogItemId,
        string ItemName,
        int Quantity,
        int DurationInMinutes,
        decimal UnitPriceAmount,
        decimal LineTotalAmount);

    private sealed record PreparedBookingData(
        Guid CustomerId,
        Guid LocationId,
        DateTimeOffset EndsAtUtc,
        int TotalDurationInMinutes,
        decimal TotalAmount,
        string CurrencyCode,
        IReadOnlyCollection<PreparedBookingLine> Lines);
}
