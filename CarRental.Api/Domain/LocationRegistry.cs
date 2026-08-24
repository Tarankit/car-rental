namespace CarRental.Api.Domain;

public sealed record Location(string Name, bool IsInternational);

/// <summary>Hardcoded location registry — the single source of truth for both the API
/// and the frontend (served via GET /cars/locations, spec.md §3/D5).</summary>
public sealed class LocationRegistry
{
    private static readonly Location[] Locations =
    [
        new("Stockholm", IsInternational: false),
        new("Gothenburg", IsInternational: false),
        new("Oslo", IsInternational: true),
        new("London", IsInternational: true),
        new("Berlin", IsInternational: true)
    ];

    public IReadOnlyList<Location> All => Locations;

    public Location? Find(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : Locations.FirstOrDefault(l => l.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
}
