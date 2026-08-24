# Car Rental Availability — SkyRoute Challenge

A car rental search & booking feature: two stub providers with different pricing rules,
normalised results, and document validation at booking time. .NET 8 Minimal API backend,
Angular frontend, xUnit tests. Runs fully offline — no real APIs, credentials, or database.

> Design and contracts were specified up front in [spec.md](spec.md) (committed before any
> implementation). AI usage is documented in [prompts.md](prompts.md); retrospective in
> [reflection.md](reflection.md).

## Prerequisites

- .NET SDK **8.0 or later** (developed with 10.0.200, project targets `net8.0`)
- Node.js **20+** (developed with v24)

## Run

Two terminals from the repo root:

```bash
# Terminal 1 — API (http://localhost:5080)
dotnet run --project CarRental.Api
```

```bash
# Terminal 2 — frontend (http://localhost:4200, proxies /cars to the API)
cd car-rental-ui
npm ci
npm start
```

Open <http://localhost:4200>.

## Test

```bash
dotnet test
```

## Project structure

```
car-rental/
├── spec.md              # data models & interface contracts (committed first)
├── CarRental.Api/       # .NET 8 Minimal API + domain + provider stubs
├── CarRental.Tests/     # xUnit tests
├── car-rental-ui/       # Angular frontend
├── prompts.md           # AI prompts & judgement calls
└── reflection.md        # what I'd improve with more time
```

## API quick reference

| Endpoint | Notes |
|---|---|
| `GET /cars/search?pickup=&from=&to=&category=` | 400 on missing params / `to <= from`; 200 sorted by total price |
| `POST /cars/book` | 422 with message on document mismatch; 201 with reference |
| `GET /cars/booking/{reference}` | 200 or 404 |
| `GET /cars/locations` | location registry (drives client-side validation) |

## Key assumptions

See the full log in [spec.md §8](spec.md). Highlights: EUR only; dates without times;
Sweden is "domestic" (Stockholm, Gothenburg) vs international (Oslo, London, Berlin);
passports are also accepted domestically; BudgetWheels' displayed per-day rate is the base
rate, with the Fri/Sat/Sun +20% night surcharge reflected in the total.
