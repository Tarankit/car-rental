# reflection.md — what I would improve with more time

## Resilience of the search fan-out

`CarSearchService` uses `Task.WhenAll` over all providers, which is correct for
deterministic in-memory stubs but wrong for real HTTP providers: one slow or failing
provider would fail (or stall) the whole search. With more time I would give each
provider call a timeout budget, catch per-provider failures, return partial results with
a "PremiumDrive is currently unavailable" notice in the payload, and add a circuit
breaker (e.g. Polly) per provider. The `ICarRentalProvider` seam is already the right
place to wrap that policy without touching the core flow.

## Availability and booking semantics

- Stub availability is **per vehicle**, not per date range (assumption A5); real
  providers would take the dates into account, and the stubs should eventually simulate
  that (e.g. a vehicle booked out for a specific week) so the UI's empty state becomes
  reachable with honest data.
- Bookings don't reserve anything: two travellers can book the same vehicle for the same
  dates. A real system needs an availability check + reservation at booking time and a
  concurrency story (optimistic locking or provider-side holds).
- The 48h free-cancellation policy is display-only; a cancellation endpoint enforcing the
  48h window against pickup time (which would also force time-of-day into the model,
  revisiting A2) is the natural next feature.

## Provider contract hardening

All provider behaviour is currently proven by tests against the two concrete stubs. I
would extract a **shared contract-test suite** that runs against any
`ICarRentalProvider` implementation (offers are priced, currency set, deterministic for
equal criteria, vehicle ids stable) so a third provider gets correctness checks for free
on day one.

## Configuration & data

Locations, catalogues, and rates are hardcoded by design (deterministic, offline). Next
step: move them to configuration/seed data so a new city or rate change is not a code
change, and make "domestic country" explicit configuration instead of assumption A3.

## Frontend

- No frontend tests: with more time, Vitest component tests for the document-rule
  validator and the sort toggle, and one Playwright happy-path E2E (the flow I verified
  manually in the browser) wired into CI.
- Currency formatting uses the default locale; a real product needs locale-aware
  formatting and (eventually) multi-currency (A1).
- Accessibility is basic-but-present (labels, aria-labels, role="alert", aria-live);
  a proper audit (focus management between views, keyboard-only walkthrough) is pending.

## Operability

- No CI: a GitHub Actions workflow running `dotnet test` and `ng build` on push would
  have caught the net8/TestHost incident (prompts.md §5) automatically.
- Observability: structured logging around provider calls and booking decisions, plus a
  health endpoint, before this ever faced a real integration.

## What I would keep

The two decisions that paid off most: writing spec.md first (the 422/400 boundaries and
the night definition never had to be relitigated), and putting pricing inside each
provider behind one interface — the test-only third provider dropped in with zero core
changes, which is exactly the extensibility the brief asks for.
