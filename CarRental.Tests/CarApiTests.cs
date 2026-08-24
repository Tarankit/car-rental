using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarRental.Tests;

/// <summary>End-to-end tests over the real HTTP pipeline (spec.md §5/§9).</summary>
public class CarApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CarApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private static object ValidBookingBody(
        string pickup = "Oslo", string documentType = "Passport",
        string provider = "BudgetWheels", string vehicleId = "BW-MIN-1") => new
        {
            providerName = provider,
            vehicleId,
            pickupLocation = pickup,
            from = "2026-09-03",
            to = "2026-09-07",
            driverName = "Anna Larsson",
            documentType,
            documentNumber = "P9988776"
        };

    [Theory]
    [InlineData("/cars/search")]                                                    // everything missing
    [InlineData("/cars/search?pickup=Stockholm&from=2026-09-04&to=2026-09-04")]     // to == from
    [InlineData("/cars/search?pickup=Stockholm&from=2026-09-07&to=2026-09-04")]     // to before from
    [InlineData("/cars/search?pickup=Paris&from=2026-09-03&to=2026-09-07")]         // unknown pickup
    [InlineData("/cars/search?pickup=Oslo&from=03-09-2026&to=2026-09-07")]          // bad date format
    [InlineData("/cars/search?pickup=Oslo&from=2026-09-03&to=2026-09-07&category=Truck")] // unknown category
    public async Task Search_returns_400_for_invalid_requests(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_returns_unified_list_sorted_by_total_without_unavailable_vehicles()
    {
        var response = await _client.GetAsync("/cars/search?pickup=Oslo&from=2026-09-03&to=2026-09-07");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offers = await response.Content.ReadFromJsonAsync<JsonElement>();

        var totals = offers.EnumerateArray().Select(o => o.GetProperty("totalPrice").GetDecimal()).ToList();
        var vehicleIds = offers.EnumerateArray().Select(o => o.GetProperty("vehicleId").GetString()).ToList();
        var providers = offers.EnumerateArray().Select(o => o.GetProperty("providerName").GetString()).ToHashSet();

        Assert.Equal(totals.OrderBy(t => t), totals);
        Assert.DoesNotContain("BW-SUV-2", vehicleIds);
        Assert.Contains("PremiumDrive", providers);
        Assert.Contains("BudgetWheels", providers);
    }

    [Fact]
    public async Task Search_category_filter_is_case_insensitive_and_respected()
    {
        var response = await _client.GetAsync("/cars/search?pickup=Oslo&from=2026-09-03&to=2026-09-07&category=suv");
        var offers = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.All(
            offers.EnumerateArray(),
            o => Assert.Equal("Suv", o.GetProperty("category").GetString()));
    }

    [Fact]
    public async Task Booking_with_wrong_document_returns_422_with_clear_message()
    {
        var response = await _client.PostAsJsonAsync("/cars/book", ValidBookingBody(documentType: "NationalId"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("International pickup 'Oslo' requires a Passport.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Booking_with_missing_document_type_returns_400_not_a_silent_default()
    {
        var response = await _client.PostAsJsonAsync("/cars/book", new
        {
            providerName = "BudgetWheels",
            vehicleId = "BW-MIN-1",
            pickupLocation = "Oslo",
            from = "2026-09-03",
            to = "2026-09-07",
            driverName = "Anna Larsson",
            documentNumber = "P9988776"
            // documentType intentionally omitted
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Booking_flow_completes_and_is_retrievable_by_reference()
    {
        var created = await _client.PostAsJsonAsync("/cars/book", ValidBookingBody());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var booking = await created.Content.ReadFromJsonAsync<JsonElement>();
        var reference = booking.GetProperty("reference").GetString();
        Assert.Matches("^CR-[A-Z0-9]{8}$", reference);
        Assert.Equal(414m, booking.GetProperty("totalPrice").GetDecimal());

        var fetched = await _client.GetAsync($"/cars/booking/{reference}");
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var fetchedBooking = await fetched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(reference, fetchedBooking.GetProperty("reference").GetString());
    }

    [Fact]
    public async Task Unknown_booking_reference_returns_404()
    {
        var response = await _client.GetAsync("/cars/booking/CR-NOPE1234");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Locations_endpoint_serves_the_registry_for_client_side_validation()
    {
        var response = await _client.GetAsync("/cars/locations");
        var locations = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(5, locations.GetArrayLength());
        Assert.Contains(
            locations.EnumerateArray(),
            l => l.GetProperty("name").GetString() == "Oslo" && l.GetProperty("isInternational").GetBoolean());
    }
}
