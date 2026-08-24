using CarRental.Api.Domain;
using CarRental.Api.Providers;
using CarRental.Api.Services;

namespace CarRental.Tests;

public class CarSearchServiceTests
{
    private static readonly SearchCriteria Criteria = new(
        "Stockholm", new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 10), null);

    private static CarSearchService DefaultService() =>
        new([new PremiumDriveProvider(), new BudgetWheelsProvider()]);

    [Fact]
    public async Task Results_are_sorted_by_total_price_ascending()
    {
        var results = await DefaultService().SearchAsync(Criteria);

        Assert.Equal(results.OrderBy(o => o.TotalPrice).Select(o => o.VehicleId), results.Select(o => o.VehicleId));
        Assert.True(results.Count >= 8);
    }

    [Fact]
    public async Task Unavailable_vehicles_never_appear_in_results()
    {
        var results = await DefaultService().SearchAsync(Criteria);

        Assert.DoesNotContain(results, o => o.VehicleId == "BW-SUV-2");
    }

    [Fact]
    public async Task Both_providers_contribute_to_the_unified_list()
    {
        var results = await DefaultService().SearchAsync(Criteria);

        Assert.Contains(results, o => o.ProviderName == PremiumDriveProvider.ProviderName);
        Assert.Contains(results, o => o.ProviderName == BudgetWheelsProvider.ProviderName);
    }

    [Fact]
    public async Task Category_filter_returns_only_that_category_from_all_providers()
    {
        var results = await DefaultService().SearchAsync(Criteria with { Category = VehicleCategory.Suv });

        Assert.NotEmpty(results);
        Assert.All(results, o => Assert.Equal(VehicleCategory.Suv, o.Category));
        Assert.Contains(results, o => o.ProviderName == PremiumDriveProvider.ProviderName);
        Assert.Contains(results, o => o.ProviderName == BudgetWheelsProvider.ProviderName);
    }

    [Fact]
    public async Task A_third_provider_plugs_into_the_core_flow_without_changes()
    {
        // The extensibility requirement: a new provider with its own pricing model
        // only needs to implement ICarRentalProvider.
        var service = new CarSearchService(
            [new PremiumDriveProvider(), new BudgetWheelsProvider(), new FlatFeeThirdProvider()]);

        var results = await service.SearchAsync(Criteria);

        var third = Assert.Single(results, o => o.ProviderName == "FlatFee");
        Assert.Equal(1m, third.TotalPrice); // cheapest — must sort first
        Assert.Equal(third, results[0]);
    }

    /// <summary>Fake provider with a deliberately different pricing model (flat 1 EUR per rental).</summary>
    private sealed class FlatFeeThirdProvider : ICarRentalProvider
    {
        public string Name => "FlatFee";

        public Task<IReadOnlyList<ProviderOffer>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
        {
            IReadOnlyList<ProviderOffer> offers =
            [
                new(new CarOffer("FlatFee", "FF-1", VehicleCategory.Economy, 1m, 1m, "EUR",
                    CancellationPolicy.NonRefundable, InsuranceType.Basic), IsAvailable: true)
            ];
            return Task.FromResult(offers);
        }
    }
}
