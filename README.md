# ElectroPi.Server

Backend API for the **ElectroPi Support Ticket Management System** — a technical assessment project implementing multi-role ticket tracking (Admin / Support Agent / Customer) with JWT authentication, strict data isolation, comment & activity timelines, time tracking, and a reporting dashboard.

Built with **ASP.NET Core 10 Web API**, **EF Core 10 / SQL Server**, **ASP.NET Core Identity**, and a layered **Clean Architecture** (Domain → Application → Infrastructure → Api).

> Companion frontend: [`ElectroPi_Client`](https://github.com/abdofathy883/ElectroPi_Client) (Angular 21).

---

## Table of Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Domain Model](#domain-model)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database & Seed Data](#database--seed-data)
- [Authentication & Authorization](#authentication--authorization)
- [API Reference](#api-reference)
- [Business Rules](#business-rules)
- [Testing](#testing)
- [Logging, Error Handling & Cross-Cutting Concerns](#logging-error-handling--cross-cutting-concerns)
- [CI/CD](#cicd)
- [Project Structure](#project-structure)
- [Assumptions & Known Limitations](#assumptions--known-limitations)
- [Demo Video](#demo-video)

---

## Architecture

The solution follows a **four-project layered/clean architecture**, keeping the domain free of framework concerns and the API layer as a thin transport shell.

```
┌─────────────────────────────────────────────────────────────┐
│  ElectroPi.Api            Controllers, middleware, DI wiring,│
│                            JWT/CORS/rate-limit configuration  │
├─────────────────────────────────────────────────────────────┤
│  ElectroPi.Infrastructure  EF Core DbContext, Identity, JWT   │
│                            issuing, service implementations,  │
│                            entity configs, migrations, seeders│
├─────────────────────────────────────────────────────────────┤
│  ElectroPi.Application     DTOs, service interfaces,          │
│                            AutoMapper profiles (no EF types    │
│                            ever cross this boundary)           │
├─────────────────────────────────────────────────────────────┤
│  ElectroPi.Domain           Entities, enums, custom exceptions,│
│                            options — zero external dependencies│
└─────────────────────────────────────────────────────────────┘
```

**Dependency rule:** `Api → Infrastructure → Application → Domain`. Domain has no reference to EF Core beyond abstractions; Application never references Infrastructure or the Api layer. Controllers depend only on `Application` interfaces (`ITicketService`, `IAuthService`, …), never on Infrastructure implementations directly — implementations are wired through `AddApplicationServices()` in `ServiceExtention.cs`.

`ElectroPi.Tests` references Application/Domain/Infrastructure directly and exercises the service layer against a real SQL Server database (see [Testing](#testing)).

## Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 (SQL Server provider) |
| Auth | ASP.NET Core Identity + JWT Bearer (access token + rotating HttpOnly-cookie refresh token) |
| Mapping | AutoMapper (entity ⇄ DTO) |
| Logging | Serilog (console + rolling daily file sink) |
| API docs | `Microsoft.AspNetCore.OpenApi` (raw OpenAPI 3 document, Development only) |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` — fixed window, 100 req/min per user/IP |
| Testing | xUnit + Moq, against a real disposable SQL Server test database |

## Domain Model

```
AppUser (Identity)                 Ticket
 ├─ FullName                        ├─ Title, Description
 ├─ IsActive, CreatedAt             ├─ Status, Priority
 ├─ Role: Admin | Agent | Customer  ├─ CustomerId → AppUser
 └─ RefreshTokens[] (owned)         ├─ AgentId? → AppUser
                                     ├─ CreatedAt / UpdatedAt / ResolvedAt / ClosedAt
                                     ├─ Comments[]       → TicketComment
                                     ├─ Activities[]     → TicketActivity (audit timeline)
                                     └─ TimeEntries[]    → TimeEntry (work logs)
```

- **TicketStatus**: `Open → Acknowledged → InProgress → Resolved → Closed`
- **TicketPriority**: `Low | Medium | High | Critical`
- **TicketActivityType**: `TicketCreation | StatusChanged | PriorityChanged | AgentAssigned | AgentUnassigned | CommentAdded`
- **UserRole**: `Admin | Agent | Customer`

Every status change, priority change, agent (re)assignment and comment is recorded as an immutable `TicketActivity` row, giving each ticket a full audit timeline. `TimeEntry` rows (work date, duration in minutes, description) roll up into the ticket's total logged time.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, SQL Server Express, or a full instance) — reachable via the connection string in `appsettings.json`
- (Optional) [Postman](https://www.postman.com/) or the built-in `.http` file for manual API testing

### Run locally

```bash
# from the repository root
cd ElectroPi_Server

# restore & build
dotnet restore
dotnet build

# apply migrations & seed data, then run the API
dotnet run --project ElectroPi.Api
```

On first run, `Program.cs` automatically:
1. runs `dbContext.Database.MigrateAsync()` — applies all pending EF Core migrations, creating the database if it doesn't exist;
2. runs `AuthSeeder` — seeds the `Admin` / `Agent` / `Customer` Identity roles plus a super-admin account;
3. runs `DbSeeder` — seeds demo users and ~30 realistic tickets with comments, activity timelines, and time entries (idempotent — skipped if tickets already exist).

The API listens on:

| Profile | URL |
|---|---|
| HTTPS (default) | `https://localhost:7085` |
| HTTP | `http://localhost:5207` |

With `ASPNETCORE_ENVIRONMENT=Development`, the raw OpenAPI document is available at `https://localhost:7085/openapi/v1.json` (no Swagger UI is wired up — import that URL into Postman/Insomnia, or use the provided `.http`/Postman collection instead).

## Configuration

Settings live in `ElectroPi.Api/appsettings.json` (and `appsettings.Development.json` for local overrides):

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<your-server>;Database=ElectroPi;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "<replace-with-a-strong-secret>",
    "Issuer": "ElectroPi",
    "Audience": "ElectroPi",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 10
  }
}
```

> ⚠️ **The committed `appsettings.json` contains a local development connection string and a non-production JWT signing key for reviewer convenience only.** They are not secrets used anywhere outside a local machine. For any real deployment, override `ConnectionStrings:DefaultConnection` and `Jwt:Key` via environment variables / user-secrets / a secret manager — never commit real credentials.

CORS is pre-configured with two named policies switched by environment: `dev` (`http://localhost:4200`, credentials allowed) and `prod` (a placeholder production origin) — update the `prod` origin to match your actual deployed frontend URL before shipping.

## Database & Seed Data

Migrations live in `ElectroPi.Infrastructure/Migrations` and are applied automatically on startup — no manual `dotnet ef database update` step is required for local development. To manage migrations manually:

```bash
cd ElectroPi_Server
dotnet ef migrations add <Name> --project ElectroPi.Infrastructure --startup-project ElectroPi.Api
dotnet ef database update --project ElectroPi.Infrastructure --startup-project ElectroPi.Api
```

### Seeded test accounts

All seeded accounts log in with their **phone number as the username** (ASP.NET Identity `UserName`), not their email.

| Role | Username (phone) | Password | Notes |
|---|---|---|---|
| Admin | `01028128912` | `Aa123#` | Primary super-admin |
| Admin | `01000000002` | `Admin@123` | Secondary admin |
| Agent | `01000000021`–`01000000024` | `Agent@123` | 4 seeded agents |
| Customer | `01000000011`–`01000000014` | `Customer@123` | 4 seeded customers, each with tickets across every status |

~30 tickets are seeded across the full status lifecycle (Open through Closed) with realistic comment threads, activity timelines, and time-entry logs, so the dashboard and list views are populated immediately after first run.

## Authentication & Authorization

- **Login** (`POST /api/auth/login`) issues a short-lived JWT **access token** (60 min, in the JSON response body) and a longer-lived **refresh token** (10 days) set as an `HttpOnly`, `Secure`, `SameSite=None` cookie scoped to `/api/auth`.
- **Refresh** (`POST /api/auth/refresh-token`) reads the refresh-token cookie, validates it isn't expired/revoked, **revokes it and rotates in a brand-new refresh token** (rotation, not reuse), and issues a fresh access token.
- **Revoke** (`POST /api/auth/revoke-token`) invalidates the current refresh token (logout).
- Role claims (`ClaimTypes.Role`) are embedded in the JWT and enforced both via `[Authorize(Roles = "...")]` on controllers/actions and via explicit checks inside services for record-level rules that a role attribute can't express (e.g. "an Agent may only see tickets assigned to them").
- The frontend's `authInterceptor` transparently retries a request once on `401` by calling the refresh endpoint, queuing concurrent requests behind a single in-flight refresh call.

### Data isolation

Every ticket-read/write path resolves the caller's id from the JWT (`ClaimTypes.NameIdentifier`) — **never from a client-supplied field** — and enforces:

- **Customer** — can only see/act on tickets where `CustomerId == callerId`.
- **Agent** — can only see/act on tickets where `AgentId == callerId`.
- **Admin** — unrestricted.

Any attempt to access a ticket outside these bounds (e.g. a Customer guessing another customer's ticket id) throws `ForbiddenException` → HTTP 403, regardless of what the client sends. This is covered explicitly by isolation tests — see [Testing](#testing).

## API Reference

Base path: `/api`. All endpoints require a valid JWT unless marked **Anonymous**. All endpoints are behind the `fixed` rate-limit policy (100 requests/minute per authenticated user or IP).

### Auth (`/api/auth`)

| Method | Route | Access | Description |
|---|---|---|---|
| POST | `/login` | Anonymous | Authenticate, returns access token + sets refresh cookie |
| POST | `/refresh-token` | Anonymous (cookie) | Rotates refresh token, returns new access token |
| POST | `/revoke-token` | Anonymous (cookie) | Revokes the current refresh token |
| POST | `/register` | Anonymous | Public self-registration — always creates a **Customer** |
| POST | `/users` | Admin | Admin-created user, any role |
| GET | `/users` | Admin | List all users |
| GET | `/user/{userId}` | Authenticated (self or Admin) | Get a single user profile |
| PATCH | `/update-user` | Authenticated | Update own profile (or any profile, if Admin) |
| DELETE | `/{userId}` | Admin | Delete a user |
| GET | `/lookup` | Authenticated | Lightweight `{id, name, role}` list, used to populate agent/customer pickers |

### Password (`/api/password`)

| Method | Route | Access | Description |
|---|---|---|---|
| PATCH | `/set-password` | Authenticated | Change own password |

### Tickets (`/api/tickets`)

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/tickets` | Authenticated | Paged/filtered/sorted list, scoped to caller's visibility |
| GET | `/ticket/{id}` | Authenticated (owner/assignee/Admin) | Ticket detail incl. comments & activity timeline |
| POST | `/ticket` | Authenticated | Create a ticket |
| PUT | `/{id}` | Authenticated (owner/assignee/Admin) | Update ticket fields |
| PATCH | `/{id}/{status}` | Authenticated | Transition ticket status (validated — see [Business Rules](#business-rules)) |
| GET | `/search/{query}` | Authenticated | Free-text search, scoped to caller's visibility |
| DELETE | `/{id}` | Admin | Delete a ticket |
| POST | `/comment` | Authenticated (owner/assignee/Admin) | Add a comment, recorded on the activity timeline |

`GET /tickets` accepts query parameters from `TicketFilterDto`: `fromDate`, `toDate`, `agentId`, `customerId`, `status`, `priority`, `sortBy`, `sortDescending`, `pageNumber` (default 1), `pageSize` (default 20, max 100) — returned wrapped in a generic `PagedResultsDto<T>` (`items`, `totalCount`, `pageNumber`, `pageSize`).

### Ticket time logs (`/api/ticketlog`)

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/` | Admin, Agent | Time entries logged by the caller |
| POST | `/` | Admin, Agent | Log work: date, duration (minutes), description |

### Reporting (`/api/ticketreporting`)

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/` | Admin | Dashboard metrics (see below) |

`GET /api/ticketreporting` returns: total/open/in-progress/resolved/closed ticket counts, open **critical** ticket count, average resolution time (hours, from creation to resolution), and a per-agent active-ticket workload breakdown — rendered on the frontend as a doughnut (status mix) and bar chart (agent workload).

A ready-to-import **Postman collection** / the raw OpenAPI JSON is the authoritative, always-up-to-date source of truth for request/response shapes.

## Business Rules

- **Status transitions are validated server-side**, independent of what the client sends:

  | From | Allowed next (non-Admin) |
  |---|---|
  | Open | Acknowledged, InProgress |
  | Acknowledged | InProgress |
  | InProgress | Resolved |
  | Resolved | Closed, InProgress (reopen) |
  | Closed | *(terminal — no further transitions)* |

  Admins may force any transition; every other role attempting an out-of-order transition receives a `400`.
- **Ticket access** is enforced per-record as described in [Data isolation](#data-isolation) — a valid JWT alone is not sufficient, the caller must also own/be assigned to the specific ticket (or be an Admin).
- **Time entries** always accumulate against the ticket regardless of who logs them, letting the API compute total time spent per ticket.
- **A short-lived `IMemoryCache` lookup cache** is used for hot, rarely-changing reference data (e.g. the ticket-agent/customer lookup used to populate pickers) to cut down on repeat round trips during a session.

## Testing

Tests live in `ElectroPi.Tests` (xUnit + Moq) and exercise `TicketService` and its collaborators **against a real, disposable SQL Server database** (`SqlServerTestDatabaseFixture`) rather than an in-memory provider — this catches issues (SQL-specific behavior, `GETUTCDATE()`, identity columns, EF query translation) that an in-memory/mocked context would silently paper over. The fixture creates a dedicated `ElectroPi_Tests` database, drops and recreates the schema before the run, and tears it down after. Point it at a different instance (e.g. in CI) via the `ELECTROPI_TEST_CONNECTION_STRING` environment variable.

```bash
cd ElectroPi_Server
dotnet test
```

Coverage focuses on the business rules that matter most for this domain:

- **Create** — validation, default status/priority, activity-log entry on creation.
- **Read** — pagination/filtering/sorting correctness, and **customer/agent data-isolation** (a customer or unassigned agent requesting another user's ticket gets `ForbiddenException`, not the ticket).
- **Update** — field updates, forbidden-access paths for non-owners.
- **ChangeStatus** — every legal/illegal transition in the table above, including the "agent not assigned to this ticket" and "customer attempting a non-customer transition" forbidden paths.
- **Delete / Search / Comment** — deletion, free-text search scoping, and comment-creation access control (including the "user without access" forbidden path).
- **TicketHelperService** — the shared access-check/status-transition helper used by the above.

> Run explanation: after the technical review call, expect to walk through a live `dotnet test` run and a small code-change exercise per the assessment brief.

## Logging, Error Handling & Cross-Cutting Concerns

- **Structured logging** via Serilog — console sink for local dev, rolling daily file sink (`logs/log-.txt`, 20 MB/file, 7-day retention) for persisted diagnostics. EF Core and ASP.NET Core internal logs are downgraded to `Warning` to keep the log readable; application logs stay at `Information`.
- **Centralized exception handling** via `ExceptionHandlingMiddleware` — maps domain exceptions to HTTP status codes consistently everywhere, so controllers stay free of try/catch noise:

  | Exception | Status |
  |---|---|
  | `NotFoundException`, `KeyNotFoundException` | 404 |
  | `UnauthorizedException`, `UnauthorizedAccessException` | 401 |
  | `ForbiddenException` | 403 |
  | `InvalidOperationException`, `ArgumentException` | 400 |
  | `NotImplementedException` | 501 |
  | *(anything else)* | 500 (logged with full stack trace) |

- **DTOs everywhere at the API boundary** — controllers and services only ever accept/return `Application.Dtos.*` types; EF entities never leave `Infrastructure`. Mapping is centralized in AutoMapper profiles (`AuthProfile`, `TicketProfile`).
- **Input validation** via Data Annotations on DTOs (`[Required]`, `[StringLength]`, `[EnumDataType]`, `[Range]` for pagination bounds), enforced automatically by `[ApiController]` model binding.
- **Rate limiting** — fixed-window limiter, 100 requests/minute, partitioned by authenticated username (falls back to remote IP for anonymous requests), returns `429` on breach.

## CI/CD

`.github/workflows/backend-ci.yml` runs on every push/PR to `main`/`develop`: restores, builds in `Release`, and runs the test suite. The pipeline is currently **scaffolded but not fully wired**: it references trait filters (`Category=Unit` / `Category=Integration`) and a publish path that don't yet match this project's structure — see [Assumptions & Known Limitations](#assumptions--known-limitations). Tightening this up (real `[Trait]` categories + a correct publish path) is the next step before relying on it as a merge gate.

## Project Structure

```
ElectroPi_Server/
├── ElectroPi.Api/                  # Controllers, Program.cs, middleware, DI extensions
│   ├── Controllers/
│   ├── Extentions/ServiceExtention.cs
│   └── Middlewares/ExceptionHandlingMiddleware.cs
├── ElectroPi.Application/          # DTOs, service interfaces, AutoMapper profiles
│   ├── Dtos/{Auth,Password,Tickets}/
│   ├── Interfaces/
│   └── MappingProfiles/
├── ElectroPi.Domain/                # Entities, Enums, Exceptions, Options — no EF/ASP.NET deps
├── ElectroPi.Infrastructure/        # EF Core DbContext, Identity, JWT issuing, service impls
│   ├── Identity/ (AuthService, JwtService, PasswordService, AuthSeeder)
│   ├── Migrations/
│   ├── Persistance/ (AppDbContext, DbSeeder, EntityConfig/)
│   └── Services/Tickets/ (TicketService, TicketLogService, TicketReportingService, TicketHelperService)
└── ElectroPi.Tests/                 # xUnit + Moq, real-SQL-Server integration-style tests
```

## Assumptions & Known Limitations

Documented transparently, per the assessment's requirement to call out incomplete requirements/assumptions:

- **No Docker Compose** — the bonus containerized setup (API + SQL Server) was not implemented in the time available; run against a local SQL Server instance instead (see [Getting Started](#getting-started)).
- **No SignalR** — real-time ticket update notifications (bonus) were not implemented; the frontend relies on request/response polling on user action.
- **No optimistic concurrency token** on `Ticket` (bonus) — concurrent edits to the same ticket currently follow last-write-wins. Ready to be discussed live: adding a `RowVersion`/`[Timestamp]` column and a `409 Conflict` path is a small, well-scoped follow-up.
- **Refresh token rotation, rate limiting, and response caching were implemented** (bonus items achieved) — see [Authentication & Authorization](#authentication--authorization) and [Business Rules](#business-rules).
- **CI pipeline is scaffolded, not production-hardened** — see [CI/CD](#cicd).
- **No Swagger/Swashbuckle UI** — only the raw OpenAPI JSON document is exposed in Development (`/openapi/v1.json`); a Postman collection is provided instead as the primary way to explore/exercise the API interactively.
- **`appsettings.json` ships a local dev connection string and JWT key** for reviewer convenience, not real secrets — see the callout in [Configuration](#configuration).
- **Frontend automated test coverage covers the tickets feature only** — see the note in [`ElectroPi_Client`](https://github.com/abdofathy883/ElectroPi_Client/README.md#testing).


## Demo Video


**[▶ Watch the demo video](https://www.loom.com/share/57ab7277ae4a44d39a676e0564a6fbf3)**
