using System.Globalization;
using CarRental.Api.Domain;
using CarRental.Api.Services;

namespace CarRental.Api.Endpoints;

/// <summary>Wire format for POST /cars/book: everything nullable so that missing fields
/// produce explicit 400 validation messages instead of silent defaults.</summary>
public sealed record BookRequestDto(
    string? ProviderName,
    string? VehicleId,
    string? PickupLocation,
    string? From,
    string? To,
    string? DriverName,
    string? DocumentType,
    string? DocumentNumber);

/// <summary>API surface per spec.md §5. Errors are RFC 7807: 400 for malformed requests,
/// 422 for semantically invalid bookings (document mismatch, unavailable offer).</summary>
public static class CarEndpoints
{
    public static IEndpointRouteBuilder MapCarEndpoints(this IEndpointRouteBuilder app)
    {
        var cars = app.MapGroup("/cars");

        cars.MapGet("/locations", (LocationRegistry locations) => TypedResults.Ok(locations.All));

        cars.MapGet("/search", SearchAsync);
        cars.MapPost("/book", BookAsync);

        cars.MapGet("/booking/{reference}", (string reference, BookingService bookings) =>
            bookings.Find(reference) is { } booking
                ? Results.Ok(booking)
                : Results.Problem(
                    title: "Booking not found",
                    detail: $"No booking with reference '{reference}'.",
                    statusCode: StatusCodes.Status404NotFound));

        return app;
    }

    private static async Task<IResult> SearchAsync(
        string? pickup, string? from, string? to, string? category,
        CarSearchService search, LocationRegistry locations, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        var location = RequireLocation(pickup, "pickup", locations, errors);
        var fromDate = RequireDate(from, "from", errors);
        var toDate = RequireDate(to, "to", errors);

        if (fromDate is { } f && toDate is { } t && t <= f)
        {
            errors["to"] = ["'to' must be after 'from'."];
        }

        VehicleCategory? categoryFilter = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (Enum.TryParse<VehicleCategory>(category, ignoreCase: true, out var parsed))
            {
                categoryFilter = parsed;
            }
            else
            {
                errors["category"] = [UnknownValueMessage("category", category, Enum.GetNames<VehicleCategory>())];
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var offers = await search.SearchAsync(
            new SearchCriteria(location!.Name, fromDate!.Value, toDate!.Value, categoryFilter), ct);
        return Results.Ok(offers);
    }

    private static async Task<IResult> BookAsync(
        BookRequestDto dto, BookingService bookings, LocationRegistry locations, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        Require(dto.ProviderName, "providerName", errors);
        Require(dto.VehicleId, "vehicleId", errors);
        Require(dto.DriverName, "driverName", errors);
        Require(dto.DocumentNumber, "documentNumber", errors);
        var location = RequireLocation(dto.PickupLocation, "pickupLocation", locations, errors);
        var fromDate = RequireDate(dto.From, "from", errors);
        var toDate = RequireDate(dto.To, "to", errors);

        if (fromDate is { } f && toDate is { } t && t <= f)
        {
            errors["to"] = ["'to' must be after 'from'."];
        }

        DocumentType? documentType = null;
        if (string.IsNullOrWhiteSpace(dto.DocumentType))
        {
            errors["documentType"] = ["The 'documentType' field is required."];
        }
        else if (Enum.TryParse<DocumentType>(dto.DocumentType, ignoreCase: true, out var parsedDoc))
        {
            documentType = parsedDoc;
        }
        else
        {
            errors["documentType"] = [UnknownValueMessage("documentType", dto.DocumentType, Enum.GetNames<DocumentType>())];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await bookings.BookAsync(
            new BookingRequest(
                dto.ProviderName!, dto.VehicleId!, location!.Name,
                fromDate!.Value, toDate!.Value,
                dto.DriverName!, documentType!.Value, dto.DocumentNumber!),
            ct);

        return result.Error switch
        {
            BookingErrorType.None => Results.Created($"/cars/booking/{result.Booking!.Reference}", result.Booking),
            // Unknown location is caught above; this arm is a safety net.
            BookingErrorType.UnknownLocation => Results.ValidationProblem(
                new Dictionary<string, string[]> { ["pickupLocation"] = [result.Message!] }),
            _ => Results.Problem(
                title: "Booking cannot be processed",
                detail: result.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity)
        };
    }

    private static void Require(string? value, string field, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"The '{field}' field is required."];
        }
    }

    private static Location? RequireLocation(
        string? value, string field, LocationRegistry locations, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"The '{field}' field is required."];
            return null;
        }

        var location = locations.Find(value);
        if (location is null)
        {
            errors[field] = [UnknownValueMessage(field, value, locations.All.Select(l => l.Name))];
        }

        return location;
    }

    private static DateOnly? RequireDate(string? value, string field, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"The '{field}' field is required."];
            return null;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        errors[field] = [$"'{value}' is not a valid date. Use yyyy-MM-dd."];
        return null;
    }

    private static string UnknownValueMessage(string field, string value, IEnumerable<string> known) =>
        $"Unknown {field} '{value}'. Valid values: {string.Join(", ", known)}.";
}
