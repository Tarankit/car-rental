using System.Text.RegularExpressions;
using CarRental.Api.Domain;
using CarRental.Api.Providers;
using CarRental.Api.Services;

namespace CarRental.Tests;

public class BookingServiceTests
{
    private static BookingService NewService() =>
        new([new PremiumDriveProvider(), new BudgetWheelsProvider()], new LocationRegistry());

    private static BookingRequest ValidRequest() => new(
        ProviderName: "BudgetWheels",
        VehicleId: "BW-MIN-1",
        PickupLocation: "Oslo",
        From: new DateOnly(2026, 9, 3),   // Thu
        To: new DateOnly(2026, 9, 7),     // Mon
        DriverName: "Anna Larsson",
        DocumentType: DocumentType.Passport,
        DocumentNumber: "P9988776");

    [Fact]
    public async Task International_pickup_with_national_id_is_rejected_with_clear_message()
    {
        var result = await NewService().BookAsync(ValidRequest() with { DocumentType = DocumentType.NationalId });

        Assert.Equal(BookingErrorType.InvalidDocument, result.Error);
        Assert.Equal("International pickup 'Oslo' requires a Passport.", result.Message);
        Assert.Null(result.Booking);
    }

    [Fact]
    public async Task Domestic_pickup_accepts_national_id()
    {
        var result = await NewService().BookAsync(ValidRequest() with
        {
            PickupLocation = "Stockholm",
            DocumentType = DocumentType.NationalId
        });

        Assert.Equal(BookingErrorType.None, result.Error);
        Assert.NotNull(result.Booking);
    }

    [Fact]
    public async Task Unknown_location_provider_and_vehicle_each_fail_with_their_error_type()
    {
        var service = NewService();

        var unknownLocation = await service.BookAsync(ValidRequest() with { PickupLocation = "Paris" });
        var unknownProvider = await service.BookAsync(ValidRequest() with { ProviderName = "NoSuchCars" });
        var unknownVehicle = await service.BookAsync(ValidRequest() with { VehicleId = "BW-XXX-9" });

        Assert.Equal(BookingErrorType.UnknownLocation, unknownLocation.Error);
        Assert.Equal(BookingErrorType.OfferUnavailable, unknownProvider.Error);
        Assert.Equal(BookingErrorType.OfferUnavailable, unknownVehicle.Error);
    }

    [Fact]
    public async Task Booking_an_unavailable_vehicle_is_rejected()
    {
        var result = await NewService().BookAsync(ValidRequest() with
        {
            PickupLocation = "Stockholm",
            VehicleId = "BW-SUV-2" // exists in the catalogue but IsAvailable = false
        });

        Assert.Equal(BookingErrorType.OfferUnavailable, result.Error);
    }

    [Fact]
    public async Task Booking_price_is_requoted_server_side_from_the_provider()
    {
        // BW-MIN-1 base 90, Thu→Mon: 90 + 108 + 108 + 108 = 414 — the request carries
        // no price at all, so the total can only come from the provider's own pricing.
        var result = await NewService().BookAsync(ValidRequest());

        Assert.Equal(BookingErrorType.None, result.Error);
        Assert.Equal(414m, result.Booking!.TotalPrice);
        Assert.Equal(CancellationPolicy.NonRefundable, result.Booking.CancellationPolicy);
    }

    [Fact]
    public async Task Reference_matches_format_and_booking_is_retrievable()
    {
        var service = NewService();

        var result = await service.BookAsync(ValidRequest());
        var reference = result.Booking!.Reference;

        Assert.Matches(new Regex("^CR-[A-Z0-9]{8}$"), reference);
        Assert.Equal(result.Booking, service.Find(reference));
        Assert.Null(service.Find("CR-NOPE1234"));
    }
}
