using System.Text.Json.Serialization;
using CarRental.Api.Domain;
using CarRental.Api.Endpoints;
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

// Enums as strings on the wire ("Economy", "Passport") — matches spec.md and the frontend models.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Car Rental API is running. See /swagger, /cars/search, /cars/book, /cars/booking/{reference}.");
app.MapCarEndpoints();

app.Run();

// Exposes the entry point to WebApplicationFactory<Program> in CarRental.Tests.
public partial class Program;
