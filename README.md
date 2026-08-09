# Child Care Licensing & Compliance System

A licensing and inspection platform modelled on how child care centres are licensed and
monitored in Ontario under the *Child Care and Early Years Act*.

Operators apply online to license a centre, the system validates requested capacity against
staffing-ratio and floor-area rules, ministry reviewers decide applications and issue
licences, inspectors and reviewers monitor compliance risk, and the public can look up any
licensed centre.

> This is a personal learning project. It is not affiliated with, endorsed by, or
> connected to the Government of Ontario.

## What it does

**Operators** sign in, see only their own centres, describe each room (age group, floor
area, proposed capacity) and submit a licence application. A rules engine validates the
request against staff-to-child ratios, maximum group sizes and minimum floor area per child,
then records the permitted capacity when the application is submitted.

**Ministry reviewers** work a queue of submitted applications, re-run the same capacity
check, request more information, reject with a reason, or approve and issue a licence. Every
transition is recorded in an application status history.

**Ministry inspectors** can open the compliance dashboard but cannot decide applications.

**The public** can search the register by city or centre name and view licence status,
expiry and inspection history, with no account required.

**A background worker** expires lapsed licences, issues renewal notices at 90, 60 and 30
days, flags centres overdue for inspection, and escalates violations past their remediation
deadline.

## Architecture

The solution is split into four projects. Project references enforce the dependency
direction at compile time — the domain cannot reach out to the database or the web.

```
ChildCareLicensing.Domain          Entities, enums, capacity rules, application workflow.
                                   No framework or database dependencies.
        ▲
ChildCareLicensing.Application     Service interfaces, use cases, DTOs.
        ▲                          Depends only on Domain.
ChildCareLicensing.Infrastructure  EF Core, Dapper, repositories, identity, background worker.
        ▲                          Implements the Application interfaces.
ChildCareLicensing.Api             Controllers, middleware, Blazor Server UI, auth policies.
```

Design decisions worth calling out:

- **The rules engine is pure.** `CapacityRulesEngine` takes room details and returns a
  result object. It has no dependencies, so the licensing rules are unit tested directly
  without a database or a web host.
- **Application workflow is explicit.** `ApplicationWorkflow` defines the allowed status
  transitions (Draft → Submitted → UnderReview → Approved / Rejected / AdditionalInfoRequired).
  Approval and licence issuance run in a single database transaction.
- **Cookie authentication with role policies.** Operators, reviewers and inspectors are
  separated by authorization policies. Operators are scoped to their own centres; API
  callers receive 401/403 instead of a redirect.
- **EF Core for writes, Dapper for reports.** Transactional work goes through EF Core with
  change tracking and migrations. Compliance reports call SQL Server stored procedures
  through Dapper so aggregation runs in the database.
- **Stored procedures ship in migrations.** `usp_FacilityComplianceSummary` and
  `usp_ViolationsByCategory` are created by an EF Core migration, so schema and procedures
  version together and CI can prove they apply to an empty database.
- **The background worker is split in two.** `LicenceMaintenanceRunner` holds the logic and
  is tested directly; `LicenceMaintenanceService` is the thin `BackgroundService` that runs
  it on a timer.

## Technology

| Area | Choice |
| --- | --- |
| Language | C# 14 on .NET 10 |
| API | ASP.NET Core Web API, OpenAPI, ProblemDetails |
| UI | Blazor Server (interactive server rendering) |
| Auth | Cookie authentication, role-based authorization policies |
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
finding, and one whose licence has already lapsed. It also seeds the demo sign-in accounts
listed below.

### Tests

```bash
dotnet test
```

The integration tests boot the real API pipeline with `WebApplicationFactory` and swap the
SQL Server context for an in-memory SQLite one, so they run in CI without a database
container. Authorization and the full review workflow (submit → request information →
resubmit → approve and issue licence) are covered there. Migrations and stored procedures
are verified separately in CI against a real SQL Server 2022 service container.

## Try it

The app runs at **http://localhost:5138**.

### Demo accounts

Password for every account: `Demo!2345`

| Role | Email | What you can do |
| --- | --- | --- |
| Operator | `maria@sunshinechildcare.example` | Sunshine centre; submit the draft application |
| Operator | `admin@maplegrove.example` | Maple Grove; already licensed |
| Ministry reviewer | `j.tremblay@ontario.example` | Application queue, decisions, compliance |
| Ministry inspector | `p.raman@ontario.example` | Compliance only; cannot decide applications |

Sign in at [/account/login](http://localhost:5138/account/login). The public register needs
no account.

### Suggested walkthrough

1. Sign in as **Maria** (operator). Open **My centres** → Sunshine Early Learning Centre.
2. Run the capacity check and submit the application.
3. Sign out, then sign in as **Julie Tremblay** (reviewer).
4. Open **Application queue**, start the review, then approve and issue a licence
   (or request more information and have the operator resubmit).
5. Confirm the centre appears on the public register at [/registry](http://localhost:5138/registry).
6. Open **Compliance** to see the stored-procedure reports.

### Web UI

| Page | Audience | Purpose |
| --- | --- | --- |
| [/](http://localhost:5138/) | Everyone | Overview |
| [/account/login](http://localhost:5138/account/login) | Operators / ministry | Sign in |
| [/facilities](http://localhost:5138/facilities) | Operator | Own centres only |
| [/facilities/.../application](http://localhost:5138/facilities/22222222-2222-2222-2222-222222222222/application) | Operator | Capacity check and submission |
| [/review](http://localhost:5138/review) | Reviewer | Application queue |
| [/reports](http://localhost:5138/reports) | Reviewer / inspector | Compliance dashboard |
| [/registry](http://localhost:5138/registry) | Public | Search licensed centres |

### JSON API

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /health` | Public | Health probe including database connectivity |
| `GET /openapi/v1.json` | Public | OpenAPI document |
| `GET /api/public/facilities?city=&name=` | Public | Register search |
| `GET /api/public/facilities/{id}` | Public | Licence status and inspection history |
| `GET /api/facilities` | Signed in | Centres (operators scoped to their own) |
| `GET /api/licence-applications/{id}` | Signed in | Application with rooms |
| `GET /api/licence-applications/{id}/validation` | Signed in | Per-room capacity check |
| `POST /api/licence-applications/{id}/submit` | Operator | Submit after validation passes |
| `GET /api/review/licence-applications/queue` | Reviewer | Applications awaiting a decision |
| `POST /api/review/licence-applications/{id}/start-review` | Reviewer | Take ownership of a submission |
| `POST /api/review/licence-applications/{id}/request-information` | Reviewer | Send back with notes |
| `POST /api/review/licence-applications/{id}/approve` | Reviewer | Approve and issue a licence |
| `POST /api/review/licence-applications/{id}/reject` | Reviewer | Reject with a reason |
| `GET /api/reports/facility-compliance` | Ministry | Risk ranking by facility |
| `GET /api/reports/violations-by-category?lookbackDays=365` | Ministry | Findings grouped by category |

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

Resume-ready for the scenarios above.

- [x] Development environment (.NET 10, SQL Server in Docker)
- [x] Layered solution and domain model
- [x] Database schema and migrations
- [x] Capacity rules engine and licence application API
- [x] Cookie authentication and role-based authorization
- [x] Reviewer queue, decisions, and licence issuance
- [x] Operator scoping (centres and applications)
- [x] Blazor UI for operators, reviewers and the public
- [x] Public register search
- [x] Background service for expiry and renewals
- [x] Compliance reporting via stored procedures
- [x] Unit and integration tests, including the review workflow
- [x] CI pipeline (GitHub Actions and Azure Pipelines)

Possible next steps: inspector-facing UI for recording visits, accessibility checks in CI,
containerized deployment, and hosting on Azure App Service.
