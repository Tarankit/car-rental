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
