using CarRental.Api.Domain;
using CarRental.Api.Providers;
using CarRental.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Both stub providers are registered against the same interface; the core services
// consume IEnumerable<ICarRentalProvider>, so a third provider is one more line here.
builder.Services.AddSingleton<ICarRentalProvider, PremiumDriveProvider>();
builder.Services.AddSingleton<ICarRentalProvider, BudgetWheelsProvider>();
builder.Services.AddSingleton<LocationRegistry>();
builder.Services.AddSingleton<CarSearchService>();
builder.Services.AddSingleton<BookingService>();

var app = builder.Build();

app.MapGet("/", () => "Car Rental API is running. See /cars/search, /cars/book, /cars/booking/{reference}.");

app.Run();
