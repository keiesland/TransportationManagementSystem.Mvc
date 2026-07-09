# TransportationManagementSystem.Mvc

A clean architecture refactor of a production payroll summarization application, demonstrating a rich domain model, 
comprehensive testing, and real bug-finding against verified production data — not just theoretical best practices.

## Overview

TransportationManagementSystem.Mvc is a payroll data import, calculation, and reporting tool built with ASP.NET Core MVC. 
Drivers' daily trip data (pickups, dropoffs, breaks) is imported from Excel, validated, summarized into paid time and 
weekly totals, and exported back to Excel for payroll processing.

The project began as a service-pattern refactor of an older imperative implementation, then went through a second, 
deeper refactor: replacing a scattered, hard-to-test calculation pipeline with a single domain model (`DriverDay`) 
that owns its own business rules. Every rule in that model — grace-period buffers, break-gap thresholds, no-show 
handling, overlapping/multi-load trip detection — was independently derived and verified against real production 
payroll output, not assumed correct by inspection.

**Key Achievement:** Found and fixed two real correctness bugs inherited from the original production code — one 
that could silently misorder a driver's week and corrupt weekly payroll totals, and one that broke pickup/dropoff 
row correspondence on multi-passenger trips. Both were caught by systematically diffing this application's output 
against real production data, not by code review alone.

## Tech Stack

- **Framework:** ASP.NET Core MVC (.NET 10)
- **ORM:** Entity Framework Core, with the InMemory provider for testing
- **Excel I/O:** NPOI (import), ClosedXML (export)
- **Testing:** xUnit, Moq
- **Architecture:** Clean Architecture — Service / Repository / Unit of Work, with a rich domain model at its core
- **Patterns:** Generic Repository, Unit of Work, Dependency Injection, Domain Model (not anemic)

## Architecture Highlights

### The DriverDay Domain Model

The centerpiece of this refactor. A driver's workday used to be calculated by threading raw `Trip` records through 
five collaborating static classes (`TripGrouper`, `AccumulatorBuilder`, `BreakDetector`, `TimeCalculator`, 
`SummaryBuilder`), each independently sorting and re-sorting parallel lists of times. That structure had a 
serious latent bug: sorting each time field (pickup, dropoff, etc.) *independently* meant a trip's pickup and its 
own dropoff could end up at different list indices once a driver had overlapping/multi-load trips — silently 
breaking row correspondence and producing incorrect break calculations.

`DriverDay` replaces all of that with a single object that computes its own `Start`, `End`, and `Breaks` as 
properties, using a running "busy until" pointer instead of independent sorts — so overlapping trips are handled 
correctly by construction, not by coincidence.

```csharp
public class DriverDay
{
    public List<TripSegment> Trips { get; set; } = new();

    public TimeSpan Start { get; }   // 30-min grace before first scheduled pickup
    public TimeSpan End { get; }     // 30-min grace after last dropoff, capped by clock-out
    public List<(TimeSpan Out, TimeSpan In)> Breaks { get; }
        // Gaps >60 min (measured on raw pickup time) qualify as a break;
        // the deducted window itself uses a 30-min buffered pickup time.
        // No-shows consume driver time without counting as a break.
}
```

Every rule embedded in this model — the 30-minute buffers at day-start, day-end, and between trips; the distinction 
between the *raw* gap (used to decide if something counts as a break) and the *buffered* gap (used to decide how 
much time gets deducted); no-show handling — was reverse-engineered from real, unlabeled production discrepancies 
and confirmed against dozens of real driver-days before being trusted.

### Clean Separation of Concerns
- **Domain layer:** `DriverDay` / `TripSegment` — pure business logic, zero database or framework dependencies
- **Service layer:** orchestration (`AggregationService`, `ValidationService`, `SummaryBuilder`, `FileImportService`)
- **Repository layer:** generic data access, `Repository<T>` behind `IRepository<T>`
- **Unit of Work:** `TripUnitOfWork` coordinates repositories and transactions, injected as `ITripUnitOfWork`
- **`IBulkOperationsProvider`:** wraps `EFCore.BulkExtensions` behind an interface, so bulk insert/delete can be 
  faked in tests (the real provider requires a relational database and doesn't work against EF Core InMemory)

### Import Validation Pipeline

File uploads are parsed and validated **before** anything touches the database:
If validation fails, the entire import is a no-op — no partial writes, no orphaned records. This is intentionally 
simple: rather than supporting in-place correction of bad rows, the user fixes the source Excel file and re-uploads. 
Validation errors are returned as an HTML fragment and injected into the page via AJAX, matching the existing 
upload UX rather than a full-page redirect.

### Bug Fixes Discovered (verified against real production output)

1. **Sort-by-surrogate-key instead of calendar date.** The original grouping logic sorted trips by `TripDateId` — a 
   database-assigned foreign key with no guaranteed relationship to calendar order. If a date's `TripDateId` was 
   assigned out of chronological order (e.g. from a later import batch), that day would silently sort to the wrong 
   position in a driver's week — displacing weekly-total resets and corrupting payroll totals for that driver. 
   Fixed by sorting on the actual `TripDate.Date` value.
2. **Independent per-column sorting breaking row correspondence.** The legacy accumulator pattern sorted each time 
   field (`ActualPickup`, `ActualDropoff`, etc.) as its own independent list. On any day with overlapping/multi-load 
   trips, a trip's pickup and its own dropoff could land at different list indices after sorting — corrupting break 
   detection and (per the original code's own comments) meaning drivers were likely being *overpaid*. Fixed by 
   `DriverDay.Breaks` tracking a single running "busy until" pointer across trips in schedule order, instead of 
   sorting each column separately.

Both bugs were found the same way: importing real (anonymized) production trip data, running it through this 
application, and diffing the output against the original production summary line by line until every discrepancy 
was explained and resolved — not left as "close enough."

## Project Structure
TransportationManagementSystem.Mvc/
├── Controllers/
├── Entities/                  # EF Core entities (Trip, Driver, TripDate, Summary)
├── Domain/                    # DriverDay, TripSegment -- framework-free business logic
├── Services/
│   ├── Interfaces/
│   ├── AggregationService.cs
│   ├── ValidationService.cs
│   ├── FileImportService.cs
│   └── ExcelExportService.cs
├── Repositories/
│   ├── IRepository.cs
│   ├── Repository.cs
│   ├── IBulkOperationsProvider.cs
│   └── EfCoreBulkOperationsProvider.cs
├── UnitOfWork/
│   ├── ITripUnitOfWork.cs
│   └── TripUnitOfWork.cs
├── Data/
│   ├── DTOs/
│   └── Query/                 # QueryOptions<T>, filtering/sorting/paging
├── Utilities/
│   ├── DriverDayGrouper.cs
│   ├── SummaryBuilder.cs
│   └── TimeFormatHelper.cs
└── Views/
TransportationManagementSystem.Mvc.Tests/
└── Unit/
├── Domain/                # DriverDay.Start/End/Breaks, against real verified data
├── Services/
├── Utilities/
└── Controllers/

## Running the Project

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or VS Code

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run the Application
```bash
dotnet run --project TransportationManagementSystem.Mvc
```

## Testing Approach

Tests are built around three ideas:

1. **The domain model is tested in isolation.** `DriverDay` has zero database or framework dependency, so its 
   `Start`/`End`/`Breaks` properties are tested with plain in-memory objects and exact expected values — no 
   mocking required.
2. **Regression tests encode the real bugs found.** Several tests are deliberately constructed to fail if either 
   the surrogate-key sorting bug or the independent-column-sort bug ever regresses (e.g. a test where `TripDateId` 
   is assigned in the *opposite* order of the real calendar dates, to catch any reversion to sorting by the FK).
3. **Persistence is tested against a real (InMemory) database, not mocked away entirely.** `FileImportService`'s 
   driver/date creation-or-reuse logic, FK linkage, and delete-before-reimport behavior are verified against an 
   actual EF Core InMemory `DbContext`, using a fake `IBulkOperationsProvider` (since the real one requires a 
   relational database).

### Example: a regression test for a real bug

```csharp
[Fact]
public void GroupTrips_SortsByCalendarDate_NotByTripDateId()
{
    // The original bug sorted by TripDateId (a surrogate FK) instead of the
    // real calendar date. TripDateId is deliberately assigned in the OPPOSITE
    // order of the real dates here -- if grouping ever reverts to sorting by
    // the surrogate key, this test fails.
    var laterDateWithLowerId = MakeTripDate(1, new DateTime(2024, 9, 24), 39);
    var earlierDateWithHigherId = MakeTripDate(2, new DateTime(2024, 9, 23), 39);
    // ...
    Assert.Equal(new DateTime(2024, 9, 23), groups[0].DriverDay.TripDate);
    Assert.Equal(new DateTime(2024, 9, 24), groups[1].DriverDay.TripDate);
}
```

## What I Learned

**Rich domain models pay for themselves in testability.** The old calculation pipeline needed a database, a 
grouping step, and an accumulator-building step just to test one break-detection edge case. `DriverDay` needs 
nothing but a few lines of object initialization — and that ease of testing is exactly what made it possible to 
pin down subtle rules (a 30-minute grace buffer measured from *whichever is later*, scheduled time or actual 
arrival) with confidence instead of guesswork.

**"It matches production" isn't the same as "it's correct."** Two separate, serious bugs were hiding in code that 
had been producing plausible-looking output for a long time. Both only surfaced by deliberately diffing against 
real data across a range of scenarios — simple days, overlapping trips, no-shows, late arrivals — rather than 
trusting that passing spot-checks meant the logic was sound.

**Fragile assumptions travel.** The surrogate-key sorting bug existed independently in two separate codebases 
(this MVC app and a newer Blazor rebuild) because both inherited the same unexamined assumption from the original 
implementation. Finding and fixing it in one didn't fix the other — it had to be independently rediscovered and 
verified in each.

**Grace-period buffers are rarely as simple as "a flat N minutes."** What looked like one 30-minute rule turned out 
to be three related-but-distinct rules (day-start buffer, day-end buffer, inter-trip buffer) plus a *separate* 
60-minute qualification threshold that isn't the same number as any of the buffers — a distinction that only 
became clear by bracketing it with real examples that fell just above and just below the line.

## License

This is a portfolio project. Feel free to use it as reference, but please create your own implementations for 
production use.

---

**Last Updated:** 07/09/2026
**Status:** DriverDay domain model refactor complete, verified against production data, full test coverage on 
domain logic, grouping, and import pipeline.
