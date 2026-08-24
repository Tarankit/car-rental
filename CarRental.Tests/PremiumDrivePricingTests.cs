using CarRental.Api.Domain;
using CarRental.Api.Providers;

namespace CarRental.Tests;

public class PremiumDrivePricingTests
{
    [Theory]
    [InlineData("PD-ECO-1", 55, 1)]
    [InlineData("PD-ECO-1", 55, 4)]
    [InlineData("PD-SUV-1", 95, 7)]
    [InlineData("PD-MIN-1", 110, 3)]
    public async Task Total_is_flat_rate_times_nights_regardless_of_weekends(
        string vehicleId, decimal rate, int nights)
    {
        // Fri 2026-09-04 start: the range always includes weekend nights,
        // proving PremiumDrive applies no surcharge.
        var from = new DateOnly(2026, 9, 4);
        var provider = new PremiumDriveProvider();

        var offers = await provider.SearchAsync(new SearchCriteria("Oslo", from, from.AddDays(nights), null));
        var offer = offers.Single(o => o.Offer.VehicleId == vehicleId).Offer;

        Assert.Equal(rate * nights, offer.TotalPrice);
        Assert.Equal(rate, offer.PerDayRate);
    }

    [Fact]
    public async Task All_offers_are_available_with_comprehensive_insurance_and_free_cancellation()
    {
        var provider = new PremiumDriveProvider();
        var offers = await provider.SearchAsync(new SearchCriteria(
            "Oslo", new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 10), null));

        Assert.All(offers, o => Assert.True(o.IsAvailable));
        Assert.All(offers, o => Assert.Equal(InsuranceType.Comprehensive, o.Offer.Insurance));
        Assert.All(offers, o => Assert.Equal(CancellationPolicy.FreeCancellation48h, o.Offer.CancellationPolicy));
    }
}
