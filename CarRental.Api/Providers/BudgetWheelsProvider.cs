using CarRental.Api.Domain;

namespace CarRental.Api.Providers;

/// <summary>Stub provider: base daily rate with a weekend surcharge — Friday, Saturday
/// and Sunday nights cost 20% more. The total is computed by iterating over each rental
/// night (explicitly NOT rate × days — spec.md §4). Basic insurance only, non-refundable,
/// and some vehicles may be unavailable. Deterministic fixed catalogue.</summary>
public sealed class BudgetWheelsProvider : ICarRentalProvider
{
    public const string ProviderName = "BudgetWheels";
    private const decimal WeekendSurchargeFactor = 1.20m;

    private sealed record Vehicle(string Id, VehicleCategory Category, decimal BaseDailyRate, bool IsAvailable);

    private static readonly Vehicle[] Catalogue =
    [
        new("BW-ECO-1", VehicleCategory.Economy, 40m, IsAvailable: true),
        new("BW-CMP-1", VehicleCategory.Compact, 50m, IsAvailable: true),
        new("BW-SUV-1", VehicleCategory.Suv, 78m, IsAvailable: true),
        new("BW-SUV-2", VehicleCategory.Suv, 70m, IsAvailable: false), // must never surface in results
        new("BW-MIN-1", VehicleCategory.Minivan, 90m, IsAvailable: true)
    ];

    public string Name => ProviderName;

    public Task<IReadOnlyList<ProviderOffer>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        IReadOnlyList<ProviderOffer> offers = Catalogue
            .Select(v => new ProviderOffer(
                new CarOffer(
                    ProviderName,
                    v.Id,
                    v.Category,
                    PerDayRate: v.BaseDailyRate, // base rate; surcharge shows in the total (spec.md P3)
                    TotalPrice: CalculateTotal(v.BaseDailyRate, criteria.From, criteria.To),
                    Currency: "EUR",
                    CancellationPolicy.NonRefundable,
                    InsuranceType.Basic),
                v.IsAvailable))
            .ToList();

        return Task.FromResult(offers);
    }

    /// <summary>Sums each night individually: a night starting on Fri/Sat/Sun costs
    /// baseRate × 1.20, any other night costs baseRate.</summary>
    private static decimal CalculateTotal(decimal baseDailyRate, DateOnly from, DateOnly to)
    {
        decimal total = 0m;
        foreach (var night in RentalPeriod.Nights(from, to))
        {
            total += IsWeekendNight(night) ? baseDailyRate * WeekendSurchargeFactor : baseDailyRate;
        }

        return total;
    }

    private static bool IsWeekendNight(DateOnly night) =>
        night.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;
}
