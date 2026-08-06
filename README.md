# Child Care Licensing & Compliance System

A licensing and inspection platform modelled on how child care centres are licensed and
monitored in Ontario under the *Child Care and Early Years Act*.

Operators apply online to license a centre, ministry staff review and approve applications,
inspectors record visits and violations, and the public can check whether a centre is
licensed and read its inspection history.

> This is a personal learning project. It is not affiliated with, endorsed by, or
> connected to the Government of Ontario.

## What it does

**Operators** register a centre, describe each room (age group, floor area, proposed
capacity) and submit a licence application. The system validates the request against
staffing ratio and floor-area rules and tells the operator what capacity is actually
permitted.

**Ministry reviewers** work a queue of submitted applications, request additional
information, and approve or reject. Approval issues a licence with an expiry date.

**Inspectors** log inspections against licensed centres, record violations with a category,
severity and remediation deadline, and close them out once resolved.

**The public** can search licensed centres and view licence status and inspection history,
with no account required.

**Overnight**, a background service expires lapsed licences, issues renewal reminders at 90,
60 and 30 days, flags centres overdue for inspection, and escalates violations past their
deadline.

## Technology

| Area | Choice |
| --- | --- |
| Language | C# on .NET 10 |
| API | ASP.NET Core Web API |
| UI | Blazor |
| Data access | Entity Framework Core, with Dapper for stored procedures |
| Database | SQL Server 2022 (Docker) |
| Testing | xUnit, integration tests via `WebApplicationFactory` |
| CI | GitHub Actions |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and
[Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
cp .env.example .env        # then set a password
docker compose up -d        # start SQL Server
set -a && source .env && set +a
dotnet run --project src/ChildCareLicensing.Api
```

Or use the helper script (waits for SQL Server to be healthy first):

```bash
./dev.sh
```

The API runs at **http://localhost:5138**. Try:

**Web UI (Blazor):**

- http://localhost:5138/
- http://localhost:5138/facilities
- http://localhost:5138/facilities/22222222-2222-2222-2222-222222222222/application

**JSON API:**

- http://localhost:5138/health
- http://localhost:5138/api/facilities
- http://localhost:5138/api/licence-applications/55555555-5555-5555-5555-555555555555
- http://localhost:5138/api/licence-applications/55555555-5555-5555-5555-555555555555/validation
- `POST` http://localhost:5138/api/licence-applications/55555555-5555-5555-5555-555555555555/submit

## Status

Under active development. See [Issues](../../issues) for the current backlog.

- [x] Development environment (.NET 10, SQL Server in Docker)
- [x] Solution structure and domain model
- [x] Database schema and migrations
- [x] Licence application API and capacity rules engine
- [x] Blazor UI
- [ ] Public lookup API
- [ ] Background service for expiry and renewals
- [ ] Reporting via stored procedures
- [ ] CI pipeline
