# prompts.md — AI usage log

Tooling: **Claude Code** (IDE-integrated CLI agent), used across the SDLC — analysis,
specification, implementation, tests, and documentation. This log records the significant
prompts and the judgement calls made around them (newest at the bottom).

---

## 1. Brief analysis & planning

**Prompt (summarised):** *"Analyze the challenge PDF and plan how we're going to solve it."*

Claude extracted the requirements and produced a phased plan. Key things it flagged that
shaped the approach:

- `spec.md` must be committed **before** any implementation file — so the first commit is
  documentation only.
- The BudgetWheels rule explicitly forbids `dailyRate × days`; pricing must iterate nights.
  We fixed a worked example up front (base 100, Thu→Mon = 460) to anchor tests.
- Extensibility is graded: pricing must live **inside each provider** behind
  `ICarRentalProvider`, so a third provider is a new class + DI registration, no core changes.

**Judgement calls made here (mine, recorded in spec.md §8):**

- A "night" = each date in `[from, to)`, surcharge keyed on the night's start date.
- Domestic pickups also accept passports (a passport is valid ID everywhere); the brief
  only says NationalId is *accepted* domestically, not *required*.
- The aggregator filters availability and sorts, providers only price — keeps the
  brief-mandated filtering rule visible and testable in the core flow.
- Booking re-quotes the price server-side rather than trusting the client's total.
- Target `net8.0` (LTS) even though SDK 10 is installed locally, for evaluator compatibility.

## 2. spec.md drafting

**Prompt (summarised):** *"Write spec.md: unified domain model, provider interface, API
contracts with status codes, location registry, assumptions log, test plan."*

Reviewed and adjusted by hand before committing: made 422 vs 400 boundaries explicit
(unknown location = 400 bad parameter; document mismatch = 422 semantic rejection), and
added `GET /cars/locations` so client-side validation is driven by server truth instead of
a duplicated hardcoded list.

## 3. Domain, providers & core services

**Prompt (summarised):** *"Implement the domain model, ICarRentalProvider, both stubs, and
the search/booking services exactly per spec.md, plus anchor tests for the surcharge rule."*

Decisions during implementation:

- The weekend rule (`IsWeekendNight`) lives **inside** `BudgetWheelsProvider`, not in a
  shared helper — it is that provider's pricing rule, and keeping it there is what makes
  the "add a third provider" story honest. Only the night-enumeration helper
  (`RentalPeriod.Nights`, dates in `[from, to)`) is shared domain vocabulary.
- Anchor tests were written **with** the implementation (full suite comes later): the
  spec.md worked example, a weekday-only stay, a single Friday night, and a full week.
  One test explicitly asserts the total is NOT `rate × days`.
- Tooling catch, not AI: this machine only has the .NET 10 runtime, so the `net8.0` target
  wouldn't launch. Added `<RollForward>LatestMajor</RollForward>` to both projects — an
  evaluator with only SDK 8 runs on 8; a machine with only a newer runtime rolls forward.
  Similarly, SDK 10's default `.slnx` solution format was replaced with a classic `.sln`
  so SDK 8 can build it.

## 4. API endpoints

**Prompt (summarised):** *"Implement the endpoints with the full 400/422/404 matrix from
spec.md §5 and verify each case with curl before committing."*

Decisions during implementation:

- Query/body primitives are bound as **strings and validated manually** instead of letting
  the framework bind `DateOnly`/enums directly. Framework binding failures produce vague
  400s; manual validation returns field-keyed `ValidationProblem` messages ("'03-09-2026'
  is not a valid date. Use yyyy-MM-dd.") and catches a subtle bug: a missing
  `documentType` would otherwise silently deserialise to the enum default (`Passport`).
- Dates accept **only** ISO `yyyy-MM-dd` (`TryParseExact`) so culture settings on the
  evaluator's machine cannot change parsing behaviour.
- Enums serialise as strings on the wire (`JsonStringEnumConverter`) to match the spec
  payloads and keep the Angular models readable.
- Verified by hand with a 14-case curl matrix (missing params, `to == from`, unknown
  pickup/category, bad date format, sorted results, category filter, international +
  NationalId → 422, unavailable vehicle → 422, booking round-trip, unknown reference →
  404, domestic + NationalId → 201) before committing. Swagger UI added for development.

## 5. Test suite — and a reversed framework decision

**Prompt (summarised):** *"Write the full suite: pricing theories (incl. month boundary),
aggregation/sort/category, document-policy matrix, booking service, and
WebApplicationFactory endpoint tests."*

47 tests. Notable choices:

- A `FlatFeeThirdProvider` fake lives in the tests purely to prove the extensibility
  claim: a third provider with a different pricing model plugs into `CarSearchService`
  with zero core changes, and sorts correctly into the unified list.
- One endpoint test pins the earlier binding decision: omitting `documentType` returns
  **400**, not a silent `Passport` default.
- The month-boundary theory (Thu 2026-10-29 → Mon 2026-11-02 = 184 at base 40) guards the
  night iterator across calendar edges. All hand-computed expected values; day-of-week
  claims in test comments were verified against a calendar before asserting.

**Reversed decision (was D6):** the original `net8.0` target was chosen for evaluator
compatibility, with `RollForward` so newer-runtime machines could still run it. The test
suite disproved that trade-off: on a .NET 10-runtime-only machine, the rolled-forward
System.Text.Json 10 requires `PipeWriter.UnflushedBytes`, which the net8 TestHost's
response writer doesn't implement — all 13 endpoint tests failed with 500s (the real
Kestrel server was unaffected, which is exactly why this would have shipped unnoticed
without integration tests). First suspicion was Swashbuckle's transitive
System.Text.Json; pinning it changed nothing — the newer STJ came from the runtime
roll-forward, not the package graph. Everything now targets `net10.0` (current LTS) with
aligned 10.x packages: one consistent target beats a nominally-compatible one that breaks
test tooling on modern machines. The brief's ".NET 8+" permits this; README states the
prerequisite.

## 6. Angular frontend

**Prompt (summarised):** *"Build the Angular UI per spec.md §6: search form fed by
/cars/locations, sortable results with all states, booking form mirroring the document
rule client-side, confirmation view."*

Decisions during implementation:

- Zoneless Angular 21 with signals end-to-end: the `App` component is a small state
  machine (`search → booking → confirmed`) over signals; components are standalone with
  `input()`/`output()` and OnPush. No router — one flow, no URLs worth bookmarking.
- The booking form's document rule is a **reactive-forms validator rebuilt from the
  pickup location input** (an `effect` re-attaches it when the pickup changes), showing
  the same message the server would return. Verified in the browser that choosing
  National ID for Oslo blocks submission with **zero network requests** — client-side
  validation is real, not decorative — and the server 422 path still renders verbatim
  under the form if it ever fires.
- The results table re-sorts client-side from a `computed()` over a sort-direction
  signal; the API's ascending order is the default.
- Honest limitation: the **empty state** is implemented but unreachable with the current
  deterministic stubs (both providers cover every category and PremiumDrive is always
  available). It exists for robustness; demoing it requires editing a stub catalogue.
- Browser-verified end-to-end: locations load, Oslo Thu→Mon search shows 8 offers sorted
  ascending (BW-MIN-1 at €414.00 matching the spec worked example, BW-SUV-2 absent),
  sort toggle flips to descending, booking completes with a CR-XXXXXXXX reference, and
  stopping the API surfaces the error banner.
