using CarRental.Api.Domain;

namespace CarRental.Tests;

public class DocumentPolicyTests
{
    // Every registered city × both document types (spec.md §3, assumption A4).
    [Theory]
    [InlineData("Stockholm", DocumentType.NationalId, true)]
    [InlineData("Stockholm", DocumentType.Passport, true)]
    [InlineData("Gothenburg", DocumentType.NationalId, true)]
    [InlineData("Gothenburg", DocumentType.Passport, true)]
    [InlineData("Oslo", DocumentType.NationalId, false)]
    [InlineData("Oslo", DocumentType.Passport, true)]
    [InlineData("London", DocumentType.NationalId, false)]
    [InlineData("London", DocumentType.Passport, true)]
    [InlineData("Berlin", DocumentType.NationalId, false)]
    [InlineData("Berlin", DocumentType.Passport, true)]
    public void International_pickups_require_a_passport_domestic_accept_both(
        string city, DocumentType document, bool expectedValid)
    {
        var location = new LocationRegistry().Find(city);

        Assert.NotNull(location);
        Assert.Equal(expectedValid, DocumentPolicy.IsValid(location!, document));
    }

    [Fact]
    public void Registry_lookup_is_case_insensitive_and_trims()
    {
        var registry = new LocationRegistry();

        Assert.NotNull(registry.Find("  oslo "));
        Assert.Null(registry.Find("Paris"));
        Assert.Null(registry.Find(null));
    }

    [Fact]
    public void Registry_defines_at_least_2_domestic_and_3_international_cities()
    {
        var registry = new LocationRegistry();

        Assert.True(registry.All.Count(l => !l.IsInternational) >= 2);
        Assert.True(registry.All.Count(l => l.IsInternational) >= 3);
    }
}
