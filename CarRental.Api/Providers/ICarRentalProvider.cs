using CarRental.Api.Domain;

namespace CarRental.Api.Providers;

/// <summary>Contract every rental provider implements (spec.md §4).
/// Each provider owns its own pricing model: <see cref="SearchAsync"/> returns offers
/// already priced per that provider's rules. Adding a provider with a different pricing
/// model is a new implementation plus a DI registration — the core flow is untouched.</summary>
public interface ICarRentalProvider
{
    string Name { get; }

    Task<IReadOnlyList<ProviderOffer>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default);
}
