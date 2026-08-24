using CarRental.Api.Domain;
using CarRental.Api.Providers;

namespace CarRental.Api.Services;

/// <summary>Core search flow (spec.md §4/D2): fans out to every registered provider,
/// filters out unavailable offers, applies the optional category filter, and returns a
/// unified list sorted by total price ascending. Provider-agnostic — a third provider
/// only needs to implement <see cref="ICarRentalProvider"/> and be registered in DI.</summary>
public sealed class CarSearchService(IEnumerable<ICarRentalProvider> providers)
{
    public async Task<IReadOnlyList<CarOffer>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        var perProvider = await Task.WhenAll(providers.Select(p => p.SearchAsync(criteria, ct)));

        return perProvider
            .SelectMany(offers => offers)
            .Where(o => o.IsAvailable)
            .Select(o => o.Offer)
            .Where(o => criteria.Category is null || o.Category == criteria.Category)
            .OrderBy(o => o.TotalPrice)
            .ToList();
    }
}
