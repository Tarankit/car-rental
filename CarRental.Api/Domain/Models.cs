namespace CarRental.Api.Domain;

/// <summary>A validated search: nights are the dates in [From, To) (spec.md D1).</summary>
public sealed record SearchCriteria(
    string PickupLocation,
    DateOnly From,
    DateOnly To,
    VehicleCategory? Category);

/// <summary>Normalised offer shown to the traveller (spec.md §2).</summary>
public sealed record CarOffer(
    string ProviderName,
    string VehicleId,
    VehicleCategory Category,
    decimal PerDayRate,
    decimal TotalPrice,
    string Currency,
    CancellationPolicy CancellationPolicy,
    InsuranceType Insurance);

/// <summary>What a provider returns: a priced offer plus its availability flag.
/// The aggregator — not the provider — filters unavailable offers (spec.md D2).</summary>
public sealed record ProviderOffer(CarOffer Offer, bool IsAvailable);

public sealed record BookingRequest(
    string ProviderName,
    string VehicleId,
    string PickupLocation,
    DateOnly From,
    DateOnly To,
    string DriverName,
    DocumentType DocumentType,
    string DocumentNumber);

public sealed record Booking(
    string Reference,
    string ProviderName,
    VehicleCategory Category,
    string PickupLocation,
    DateOnly From,
    DateOnly To,
    string DriverName,
    DocumentType DocumentType,
    decimal TotalPrice,
    string Currency,
    CancellationPolicy CancellationPolicy);
