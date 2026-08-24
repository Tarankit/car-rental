using System.Collections.Concurrent;
using CarRental.Api.Domain;
using CarRental.Api.Providers;

namespace CarRental.Api.Services;

public enum BookingErrorType
{
    None,
    /// <summary>Pickup location is not in the registry → endpoint maps to 400.</summary>
    UnknownLocation,
    /// <summary>Document type not valid for the pickup location → 422 (spec.md §5).</summary>
    InvalidDocument,
    /// <summary>Unknown provider/vehicle, or vehicle not available → 422 (spec.md D3).</summary>
    OfferUnavailable
}

public sealed record BookingResult(Booking? Booking, BookingErrorType Error, string? Message)
{
    public static BookingResult Success(Booking booking) => new(booking, BookingErrorType.None, null);
    public static BookingResult Fail(BookingErrorType error, string message) => new(null, error, message);
}

/// <summary>Booking flow (spec.md §5/§7): validates the document against the pickup
/// location, re-quotes the price from the provider (never trusts a client total — D4),
/// and stores bookings in memory only.</summary>
public sealed class BookingService(IEnumerable<ICarRentalProvider> providers, LocationRegistry locations)
{
    private readonly ConcurrentDictionary<string, Booking> _bookings = new(StringComparer.OrdinalIgnoreCase);

    public async Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        var pickup = locations.Find(request.PickupLocation);
        if (pickup is null)
        {
            return BookingResult.Fail(
                BookingErrorType.UnknownLocation,
                $"Unknown pickup location '{request.PickupLocation}'.");
        }

        if (!DocumentPolicy.IsValid(pickup, request.DocumentType))
        {
            return BookingResult.Fail(BookingErrorType.InvalidDocument, DocumentPolicy.MismatchMessage(pickup));
        }

        var provider = providers.FirstOrDefault(p =>
            p.Name.Equals(request.ProviderName, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            return BookingResult.Fail(
                BookingErrorType.OfferUnavailable,
                $"Unknown provider '{request.ProviderName}'.");
        }

        // Re-quote at booking time from the provider's own pricing.
        var criteria = new SearchCriteria(pickup.Name, request.From, request.To, Category: null);
        var offers = await provider.SearchAsync(criteria, ct);
        var offer = offers.FirstOrDefault(o =>
            o.Offer.VehicleId.Equals(request.VehicleId, StringComparison.OrdinalIgnoreCase));

        if (offer is null || !offer.IsAvailable)
        {
            return BookingResult.Fail(
                BookingErrorType.OfferUnavailable,
                $"Vehicle '{request.VehicleId}' is not available from {provider.Name}.");
        }

        var booking = new Booking(
            Reference: NewReference(),
            ProviderName: provider.Name,
            Category: offer.Offer.Category,
            PickupLocation: pickup.Name,
            From: request.From,
            To: request.To,
            DriverName: request.DriverName.Trim(),
            DocumentType: request.DocumentType,
            TotalPrice: offer.Offer.TotalPrice,
            Currency: offer.Offer.Currency,
            CancellationPolicy: offer.Offer.CancellationPolicy);

        while (!_bookings.TryAdd(booking.Reference, booking))
        {
            booking = booking with { Reference = NewReference() };
        }

        return BookingResult.Success(booking);
    }

    public Booking? Find(string reference) =>
        _bookings.TryGetValue(reference, out var booking) ? booking : null;

    /// <summary>"CR-" + 8 uppercase alphanumerics (spec.md §2).</summary>
    private static string NewReference() =>
        $"CR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
