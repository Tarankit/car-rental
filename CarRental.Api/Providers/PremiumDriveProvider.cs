using CarRental.Api.Domain;

namespace CarRental.Api.Providers;

/// <summary>Stub provider: flat daily rate (total = rate × nights), comprehensive
/// insurance included, free cancellation up to 48h, always available.
/// Deterministic fixed catalogue — no randomness, no clock reads.</summary>
public sealed class PremiumDriveProvider : ICarRentalProvider
{
    public const string ProviderName = "PremiumDrive";

    private sealed record Vehicle(string Id, VehicleCategory Category, decimal DailyRate);

    private static readonly Vehicle[] Catalogue =
    [
        new("PD-ECO-1", VehicleCategory.Economy, 55m),
        new("PD-CMP-1", VehicleCategory.Compact, 65m),
        new("PD-SUV-1", VehicleCategory.Suv, 95m),
        new("PD-MIN-1", VehicleCategory.Minivan, 110m)
    ];

    public string Name => ProviderName;

    public Task<IReadOnlyList<ProviderOffer>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        var nights = RentalPeriod.NightCount(criteria.From, criteria.To);

        IReadOnlyList<ProviderOffer> offers = Catalogue
            .Select(v => new ProviderOffer(
                new CarOffer(
                    ProviderName,
                    v.Id,
                    v.Category,
                    PerDayRate: v.DailyRate,
                    TotalPrice: v.DailyRate * nights,
                    Currency: "EUR",
                    CancellationPolicy.FreeCancellation48h,
                    InsuranceType.Comprehensive),
                IsAvailable: true))
            .ToList();

        return Task.FromResult(offers);
    }
}
