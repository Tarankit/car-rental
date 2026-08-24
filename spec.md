# spec.md — Car Rental Availability (SkyRoute)

> This specification is committed **before any implementation files**. It defines the data
> models, interface contracts, API contracts, and the judgement calls / assumptions the
> implementation will follow.

## 1. Overview

A traveller searches for rental cars by pickup location, date range, and optional vehicle
category. The system queries two stub providers (**PremiumDrive**, **BudgetWheels**),
applies each provider's pricing rules, filters out unavailable vehicles, normalises the
results into a unified shape, and returns them sorted by total price. The traveller then
books a vehicle; document validation is applied at booking time (client-side and
server-side).

No real provider APIs, no persistence, no auth. Runs fully offline.

## 2. Domain model (unified)

All money values are `decimal`, single currency **EUR** (assumption A1).
All dates are `DateOnly` (no times; pickup/return time-of-day is out of scope — A2).

### Enums

| Enum | Values | Notes |
|---|---|---|
| `VehicleCategory` | `Economy`, `Compact`, `Suv`, `Minivan` | Unified across providers; each provider maps its own naming |
| `CancellationPolicy` | `FreeCancellation48h`, `NonRefundable` | PremiumDrive → free up to 48h before pickup; BudgetWheels → non-refundable |
| `InsuranceType` | `Comprehensive`, `Basic` | PremiumDrive includes Comprehensive in the quoted price; BudgetWheels offers Basic only |
| `DocumentType` | `Passport`, `NationalId` | Used at booking time |

### Records

```csharp
record SearchCriteria(string PickupLocation, DateOnly From, DateOnly To, VehicleCategory? Category);

record CarOffer(
    string ProviderName,        // "PremiumDrive" | "BudgetWheels"
    string VehicleId,           // provider-scoped stable id, e.g. "PD-ECO-1"
    VehicleCategory Category,
    decimal PerDayRate,         // provider's base daily rate (see pricing note P3)
    decimal TotalPrice,         // full rental price per provider rules
    string Currency,            // "EUR"
    CancellationPolicy CancellationPolicy,
    InsuranceType Insurance);

record BookingRequest(
    string ProviderName,
    string VehicleId,
    string PickupLocation,
    DateOnly From,
    DateOnly To,
    string DriverName,
    DocumentType DocumentType,
    string DocumentNumber);

record Booking(
    string Reference,           // "CR-" + 8 uppercase alphanumerics
    string ProviderName,
    VehicleCategory Category,
    string PickupLocation,
    DateOnly From,
    DateOnly To,
    string DriverName,
    DocumentType DocumentType,
    decimal TotalPrice,
    string Currency,
    CancellationPolicy CancellationPolicy);
```

## 3. Locations & document rules

Hardcoded location registry (single source of truth in the API; the frontend fetches it —
see `GET /cars/locations`). "Domestic" is defined relative to the operating country,
**Sweden** (A3).

| City | Kind |
|---|---|
| Stockholm | Domestic |
| Gothenburg | Domestic |
| Oslo | International |
| London | International |
| Berlin | International |

Document validation at booking time:

- **International** pickup → `Passport` **required**; `NationalId` → **422**.
- **Domestic** pickup → `NationalId` accepted; `Passport` **also accepted** (A4 — a
  passport is valid identification everywhere).
- Unknown pickup location → **400** (invalid request parameter, both on search and book).

## 4. Provider contract (extensibility)

```csharp
public interface ICarRentalProvider
{
    string Name { get; }
    Task<IReadOnlyList<ProviderOffer>> SearchAsync(SearchCriteria criteria, CancellationToken ct);
}
```

- `ProviderOffer` is `CarOffer` **plus** `bool IsAvailable`.
- **Each provider owns its own pricing**: `SearchAsync` returns offers already priced per
  that provider's rules. Adding a third provider with a different pricing model = one new
  class registered in DI; the core flow (aggregation, filtering, normalisation, sorting,
  booking) does not change.
- Both stub implementations are registered in DI against `ICarRentalProvider`; the
  aggregation service takes `IEnumerable<ICarRentalProvider>`.
- The **aggregator** (not the providers) filters out `IsAvailable == false` offers, applies
  the optional category filter, and sorts by `TotalPrice` ascending (D2).

### Provider rules

| | PremiumDrive | BudgetWheels |
|---|---|---|
| Pricing | Flat daily rate: `total = rate × nights` | Base daily rate + weekend surcharge (below) |
| Insurance | Comprehensive, included in quoted price | Basic only |
| Cancellation | Free up to 48h before pickup | Non-refundable |
| Availability | Always available | May return unavailable vehicles → filtered out |

### Weekend surcharge (BudgetWheels only)

- A rental of `[from, to)` consists of **nights**; each night is identified by its start
  date. `from = Thu 2026-09-03, to = Mon 2026-09-07` → nights Thu, Fri, Sat, Sun (4).
- A night whose date falls on **Friday, Saturday, or Sunday** costs `baseRate × 1.20m`;
  other nights cost `baseRate`.
- Total = **sum over each night** (explicitly NOT `rate × days`).
- Worked example: base 100, Thu→Mon = 100 + 120 + 120 + 120 = **460.00**.

**P3 — displayed per-day rate:** for BudgetWheels, `PerDayRate` is the **base** daily rate;
the surcharge is reflected only in `TotalPrice`. For PremiumDrive, `PerDayRate × nights ==
TotalPrice` always.

### Stub determinism

Stubs hold fixed in-memory catalogues — no randomness, no clock reads. Representative
scenarios guaranteed:

- Every `VehicleCategory` appears in both providers' catalogues.
- BudgetWheels includes at least one vehicle with `IsAvailable = false` (must never appear
  in results).
- Rates are distinct across providers so the sort order is provable in tests.

## 5. API contract

Base route: `/cars`. Errors use RFC 7807 `ProblemDetails` (`Results.ValidationProblem` /
`Results.Problem`) with a human-readable `detail`.

### `GET /cars/search?pickup={location}&from={date}&to={date}&category={category}`

| Case | Response |
|---|---|
| `pickup`, `from`, or `to` missing / unparseable | **400** |
| `to` not strictly after `from` | **400** |
| Unknown `pickup` location | **400** |
| Unknown `category` value | **400** |
| Valid | **200** → `CarOffer[]` sorted by `TotalPrice` asc (may be empty `[]`) |

`category` is optional; dates are ISO `yyyy-MM-dd`.

### `POST /cars/book`

Body: `BookingRequest` (JSON).

| Case | Response |
|---|---|
| Missing/blank required field, invalid dates, unknown pickup | **400** |
| Document type not valid for pickup location kind | **422** with clear message, e.g. `"International pickup 'Oslo' requires a Passport."` |
| Unknown provider/vehicle, or vehicle not available | **422** with clear message (D3) |
| Valid | **201** → `Booking` with `reference` |

The server **re-quotes the price at booking time** from the provider (never trusts a
client-supplied total — D4).

### `GET /cars/booking/{reference}`

| Case | Response |
|---|---|
| Known reference | **200** → `Booking` |
| Unknown reference | **404** |

### `GET /cars/locations` (helper)

**200** → `[{ "name": "Stockholm", "isInternational": false }, ...]` — lets the frontend
share the location registry and drive client-side document validation from server truth (D5).

## 6. Frontend contract (Angular)

- **Search form**: pickup (select, from `/cars/locations`), from/to dates, optional
  category. Client-side: required fields, `to > from`.
- **Results**: provider badge, category, per-day rate, total price, cancellation policy,
  insurance indicator; **sortable by total price** (asc/desc). States: loading, results,
  **empty**, **error**.
- **Booking form**: driver name, document type, document number. Client-side rule mirrors
  the server: international pickup → passport required (validated before submit); server
  422 messages are displayed if they occur anyway.
- **Confirmation**: reference number, provider, total price, cancellation policy.

## 7. Bookings storage

In-memory `ConcurrentDictionary<string, Booking>` behind a `BookingService` (singleton).
Bookings do not survive restart — explicitly out of scope per brief.

## 8. Assumptions & decisions log

| # | Decision |
|---|---|
| A1 | Single currency EUR; no conversion. |
| A2 | Dates only (`DateOnly`); pickup/return times out of scope. The 48h free-cancellation window is a display/policy label only — cancellation endpoints are out of scope. |
| A3 | Operating country is Sweden: Stockholm/Gothenburg domestic; Oslo/London/Berlin international. |
| A4 | Passport is accepted for domestic pickups too; NationalId is rejected for international. |
| A5 | Same catalogue at every location (stubs don't vary by city); availability varies per vehicle, not per date. |
| D1 | Nights = dates in `[from, to)`; surcharge keyed on the night's start date being Fri/Sat/Sun. |
| D2 | Aggregator (core flow) filters unavailability and sorts; providers only price. |
| D3 | Booking a vehicle that is unknown or unavailable → 422 (unprocessable booking), not 404. |
| D4 | Booking price is re-quoted server-side. |
| D5 | Location registry is served by the API so client and server validate from one source of truth. |
| D6 | ~~Backend targets `net8.0` (LTS) for maximum evaluator compatibility.~~ **Revised during Phase 4:** targets `net10.0` (current LTS). Running net8.0 test-host assemblies on a rolled-forward newer runtime breaks response serialization (`PipeWriter.UnflushedBytes`), so the "compatible" target was actually fragile on modern machines; one consistent current target is more robust. Rationale in prompts.md §5. |

## 9. Test plan (core business logic)

- BudgetWheels pricing: weekday-only stay; single Fri night; Thu→Mon worked example (460);
  full week; range crossing a month boundary.
- PremiumDrive: flat `rate × nights`.
- Availability: `IsAvailable=false` never surfaces; PremiumDrive always present.
- Normalisation/sort: mixed-provider results sorted by total; category filter respected.
- Document validation: domestic+NationalId ✓, domestic+Passport ✓, international+Passport ✓,
  international+NationalId ✗ 422 — for each registered city.
- Endpoint tests (`WebApplicationFactory`): 400 matrix, 422 message, booking round-trip
  (book → fetch by reference), 404 unknown reference.
