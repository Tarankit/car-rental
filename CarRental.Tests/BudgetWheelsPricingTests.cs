using CarRental.Api.Domain;
using CarRental.Api.Providers;

namespace CarRental.Tests;

/// <summary>Anchor tests for the weekend surcharge rule (spec.md §4): Fri/Sat/Sun nights
/// cost 20% more, computed by iterating each night — never rate × days.</summary>
public class BudgetWheelsPricingTests
{
    private static async Task<decimal> TotalFor(string vehicleId, DateOnly from, DateOnly to)
    {
        var provider = new BudgetWheelsProvider();
        var offers = await provider.SearchAsync(new SearchCriteria("Stockholm", from, to, null));
        return offers.Single(o => o.Offer.VehicleId == vehicleId).Offer.TotalPrice;
    }

    [Fact]
    public async Task Weekday_only_stay_has_no_surcharge()
    {
        // Mon 2026-09-07 → Thu 2026-09-10: nights Mon, Tue, Wed (3) at base 40.
        var total = await TotalFor("BW-ECO-1", new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 10));
        Assert.Equal(120m, total);
    }

    [Fact]
    public async Task Single_friday_night_costs_20_percent_more()
    {
        // Fri 2026-09-04 → Sat 2026-09-05: one Friday night, 40 × 1.20.
        var total = await TotalFor("BW-ECO-1", new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 5));
        Assert.Equal(48m, total);
    }

    [Fact]
    public async Task Thursday_to_monday_matches_hand_computed_example()
    {
        // Spec.md worked example: base 100 → Thu + Fri + Sat + Sun = 100 + 120 + 120 + 120 = 460.
        // BW-MIN-1 base 90: 90 + 108 + 108 + 108 = 414. Thu 2026-09-03 → Mon 2026-09-07.
        var total = await TotalFor("BW-MIN-1", new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 7));
        Assert.Equal(414m, total);
        Assert.NotEqual(90m * 4, total); // the forbidden rate × days shortcut would give 360
    }

    [Fact]
    public async Task Full_week_applies_surcharge_to_exactly_three_nights()
    {
        // Mon 2026-09-07 → Mon 2026-09-14: 7 nights, Fri+Sat+Sun surcharged.
        // 50 × 4 + 60 × 3 = 380 for BW-CMP-1.
        var total = await TotalFor("BW-CMP-1", new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 14));
        Assert.Equal(380m, total);
    }
}
