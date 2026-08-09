# Child Care Licensing & Compliance System

A licensing and inspection platform modelled on how child care centres are licensed and
monitored in Ontario under the *Child Care and Early Years Act*.

Operators apply online to license a centre, the system validates the requested capacity
against staffing-ratio and floor-area rules, inspectors record visits and violations,
ministry staff track compliance risk, and the public can look up any centre.

> This is a personal learning project. It is not affiliated with, endorsed by, or
> connected to the Government of Ontario.

## What it does

**Operators** register a centre, describe each room (age group, floor area, proposed
capacity) and submit a licence application. A rules engine validates the request against
staff-to-child ratios, maximum group sizes and minimum floor area per child, then tells the
operator exactly what capacity is permitted and why.

**Ministry staff** get a compliance dashboard that ranks centres by risk: overdue findings,
lapsed licences, upcoming expiries and centres that have gone more than a year without an
inspection.

**The public** can search the register by city or centre name and view licence status,
expiry and inspection history, with no account required.

**A background worker** expires lapsed licences, issues renewal notices at 90, 60 and 30
days, flags centres overdue for inspection, and escalates violations past their remediation
deadline.

## Architecture

The solution is split into four projects, and the project references enforce the dependency
direction at compile time — the domain cannot reach out to the database or the web.

```
ChildCareLicensing.Domain          Entities, enums, and the capacity rules engine.
                                   No framework or database dependencies.
        ▲
ChildCareLicensing.Application     Service interfaces, use cases, DTOs.
        ▲                          Depends only on Domain.
ChildCareLicensing.Infrastructure  EF Core, Dapper, repositories, background worker.
        ▲                          Implements the Application interfaces.
ChildCareLicensing.Api             Controllers, middleware, Blazor Server UI, DI wiring.
```

A few decisions worth calling out:

- **The rules engine is pure.** `CapacityRulesEngine` takes room details and returns a
  result object. It has no dependencies, so the licensing rules are unit tested directly
  without a database or a web host.
- **EF Core for writes, Dapper for reports.** Transactional work goes through EF Core with
  change tracking and migrations. The two compliance reports call SQL Server stored
  procedures through Dapper so the aggregation runs in the database instead of pulling rows
  into application memory.
- **Stored procedures ship in migrations.** `usp_FacilityComplianceSummary` and
  `usp_ViolationsByCategory` are created by an EF Core migration, so the schema and the
  procedures version together and CI can prove they apply to an empty database.
- **The background worker is split in two.** `LicenceMaintenanceRunner` holds the logic and
  is tested directly; `LicenceMaintenanceService` is the thin `BackgroundService` that runs
  it on a timer.

## Technology

| Area | Choice |
| --- | --- |
| Language | C# 14 on .NET 10 |
| API | ASP.NET Core Web API, OpenAPI, ProblemDetails |
| UI | Blazor Server (interactive server rendering) |
| Data access | Entity Framework Core 10, Dapper for stored procedures |
| Database | SQL Server 2022 (Docker) |
| Testing | xUnit, `WebApplicationFactory` integration tests over SQLite |
| CI | GitHub Actions and Azure Pipelines |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and
[Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
cp .env.example .env        # then set a password
./dev.sh                    # starts SQL Server, waits for it, runs the app
```

`dev.sh` waits for the SQL Server container health check before starting the app, because
SQL Server accepts TCP connections a few seconds before it can actually serve queries.

To run the pieces yourself instead:

```bash
docker compose up -d
set -a && source .env && set +a
dotnet run --project src/ChildCareLicensing.Api
```

On first run the app applies migrations and seeds a demo register with four centres: one
still drafting an application, one approaching renewal, one with an overdue critical
finding, and one whose licence has already lapsed.

### Tests

```bash
dotnet test
```

The integration tests boot the real API pipeline with `WebApplicationFactory` and swap the
SQL Server context for an in-memory SQLite one, so they run in CI without a database
container. The migrations and stored procedures are verified separately in CI against a
real SQL Server 2022 service container.

## Try it

The app runs at **http://localhost:5138**.

**Web UI**

| Page | What it shows |
| --- | --- |
| [/](http://localhost:5138/) | Overview |
| [/facilities](http://localhost:5138/facilities) | Operator view of registered centres |
| [/facilities/22222222-2222-2222-2222-222222222222/application](http://localhost:5138/facilities/22222222-2222-2222-2222-222222222222/application) | Capacity validation and submission |
| [/registry](http://localhost:5138/registry) | Public search of licensed centres |
| [/reports](http://localhost:5138/reports) | Compliance dashboard (stored procedures) |

**JSON API**

| Endpoint | Purpose |
| --- | --- |
| `GET /health` | Health probe including database connectivity |
| `GET /openapi/v1.json` | OpenAPI document |
| `GET /api/facilities` | Registered centres |
| `GET /api/licence-applications/{id}` | Application with its rooms |
| `GET /api/licence-applications/{id}/validation` | Per-room capacity check |
| `POST /api/licence-applications/{id}/submit` | Submit after validation passes |
| `GET /api/public/facilities?city=&name=` | Anonymous register search |
| `GET /api/public/facilities/{id}` | Licence status and inspection history |
| `GET /api/reports/facility-compliance` | Risk ranking by facility |
| `GET /api/reports/violations-by-category?lookbackDays=365` | Findings grouped by category |

Sample walkthrough:

```bash
APP=http://localhost:5138
APPLICATION=55555555-5555-5555-5555-555555555555

curl -s "$APP/api/licence-applications/$APPLICATION/validation"
curl -s -X POST "$APP/api/licence-applications/$APPLICATION/submit"
curl -s "$APP/api/reports/facility-compliance"
```

## The licensing rules

Simplified from the age-group requirements under the *Child Care and Early Years Act*. Each
room is checked against all three limits, and the lowest one wins.

| Age group | Staff : children | Max group size | Min floor area per child |
| --- | --- | --- | --- |
| Infant | 1 : 3 | 12 | 2.5 m² |
| Toddler | 1 : 5 | 15 | 2.5 m² |
| Preschool | 1 : 8 | 16 | 2.8 m² |
| School age | 1 : 15 | 26 | 2.8 m² |

A 45 m² infant room asking for 12 children passes the floor-area limit (45 ÷ 2.5 = 18) and
the group-size limit (12), so it is licensed for 12 and requires 4 staff. The same room
asking for 14 is rejected with the specific rule it breaks.

## Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs three jobs on every push and
pull request:

1. **Build and test** — restore, build in Release, run all tests with coverage.
2. **Dependency scan** — fails the build if any package has a known vulnerability.
3. **Migrations and stored procedures** — starts SQL Server 2022 as a service container,
   applies every migration to an empty database, then executes both reporting procedures to
   prove they are valid.

[`azure-pipelines.yml`](azure-pipelines.yml) mirrors the build, test and scan stages for
Azure DevOps.

## Status

Feature complete for the scenarios above.

- [x] Development environment (.NET 10, SQL Server in Docker)
- [x] Layered solution and domain model
- [x] Database schema and migrations
- [x] Licence application API and capacity rules engine
- [x] Blazor UI
- [x] Public lookup API and register search
- [x] Background service for expiry and renewals
- [x] Reporting via stored procedures
- [x] CI pipeline

Possible next steps: authentication and role-based authorization for the reviewer queue,
an inspector-facing UI for recording visits, and deployment to Azure App Service.
