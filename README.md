# Newsletter Preference System

A small full-stack application for capturing, validating, persisting, and managing newsletter subscription preferences. Submitted as part of the Full Stack Developer recruitment exercise for SDS / Aspire Software.

> **Status:** Backend complete (.NET 10 API + SQL Server + 60 passing tests). React + TypeScript frontend is the next phase.

---

## Tech stack

| Layer          | Choice                                                                |
| -------------- | --------------------------------------------------------------------- |
| API            | ASP.NET Core 10, controllers, FluentValidation 12                     |
| Persistence    | EF Core 10 + SQL Server (Microsoft.EntityFrameworkCore.SqlServer)     |
| Architecture   | Clean Architecture — Domain / Application / Infrastructure / Api      |
| Testing        | xUnit, Moq, FluentAssertions, `WebApplicationFactory<Program>`, EF InMemory |
| Local infra    | Docker Compose (SQL Server 2022)                                      |

The solution file is the new XML-format [NPS.slnx](NPS.slnx).

## Solution structure

```
NewsletterPreferences.Domain          ← entities, value objects, interfaces — no external deps
NewsletterPreferences.Application     ← services, DTOs, validators, Result<T>
NewsletterPreferences.Infrastructure  ← EF Core, repositories, migrations, AppDbContext
NewsletterPreferences.Api             ← controllers, filters, middleware, Program.cs
tests/
  NewsletterPreferences.Application.Tests   ← unit tests (38)
  NewsletterPreferences.Api.Tests           ← integration tests via TestWebApplicationFactory (22)
```

Project references enforce the dependency direction: Api depends on Application + Infrastructure; Infrastructure depends on Application + Domain; Application depends only on Domain.

A deeper architectural note for contributors is in [CLAUDE.md](CLAUDE.md).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the SQL Server container)

## Quick start

```powershell
# 1. Copy the env file and (optionally) change the password
copy .env.example .env

# 2. Start SQL Server
docker compose up -d

# 3. Apply EF Core migrations
dotnet ef database update --project NewsletterPreferences.Infrastructure --startup-project NewsletterPreferences.Api

# 4. Run the API
dotnet run --project NewsletterPreferences.Api
```

Swagger UI is served at `https://localhost:<port>/swagger` in Development.

If you change `SA_PASSWORD` in `.env`, update the matching password in [NewsletterPreferences.Api/appsettings.json](NewsletterPreferences.Api/appsettings.json) (`ConnectionStrings:DefaultConnection`) so the API can connect.

## Configuration

All in [appsettings.json](NewsletterPreferences.Api/appsettings.json):

| Key                            | Purpose                                                      |
| ------------------------------ | ------------------------------------------------------------ |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string                          |
| `AdminSettings:ApiKey`         | Value the admin endpoints expect in the `X-Admin-Key` header |
| `Cors:AllowedOrigins`          | Array of frontend origins allowed by CORS                    |

The committed admin key is a placeholder (`change-this-in-production`). For any non-local deployment, override it via environment variable (`AdminSettings__ApiKey=...`) or user secrets — never commit a real one.

## API surface

### Public

| Method | Route                  | Notes                                                   |
| ------ | ---------------------- | ------------------------------------------------------- |
| `POST` | `/api/subscriptions`   | Upsert by email. `201 Created` for new, `200 OK` (`IsUpdate=true`) for an existing email. Rate-limited (10 requests / minute / IP, fixed-window). |
| `GET`  | `/api/lookups`         | Returns subscriber types, communication preferences, and newsletter interests for populating frontend dropdowns. |

### Admin (header `X-Admin-Key: <key>`)

| Method   | Route                                | Notes                                              |
| -------- | ------------------------------------ | -------------------------------------------------- |
| `GET`    | `/api/admin/subscriptions`           | Paged list. Query: `searchTerm`, `subscriberTypeId`, `communicationPreferenceId`, `interestId`, `page`, `pageSize` (clamped 1–100). |
| `GET`    | `/api/admin/subscriptions/{id}`      | Get a single subscription.                         |
| `DELETE` | `/api/admin/subscriptions/{id}`      | Hard delete.                                       |

Without (or with a wrong) `X-Admin-Key`, admin endpoints return `401`.

## Validation

Two layers — both server-side:

1. **`UpsertSubscriptionRequestValidator`** (FluentValidation) — required fields, max lengths, email format, non-empty preference/interest lists, consent must be `true`.
2. **Conditional + referential checks inside `SubscriptionService.UpsertAsync`** — runs only if (1) passes:
   - Selecting *Phone* or *SMS* requires a phone number.
   - Selecting *Post* requires a postal address.
   - All submitted lookup IDs must exist in the database.

The `Email` value object additionally enforces a stricter regex via `Email.Create`; failures surface as validation errors, not exceptions.

Client-side validation will be added in the React phase.

## Testing

```powershell
# Run everything
dotnet test NPS.slnx

# One project at a time
dotnet test tests/NewsletterPreferences.Application.Tests
dotnet test tests/NewsletterPreferences.Api.Tests

# A single test
dotnet test NPS.slnx --filter "FullyQualifiedName~AdminControllerTests.GetPaged_WithoutAdminKey_Returns401"
```

**60 tests pass** (38 unit + 22 integration). Coverage includes:

- Validator rules (empty / over-length / invalid email / missing consent).
- `SubscriptionService` upsert flow (new + duplicate-email update, conditional PHONE/SMS/POST rules, invalid lookup IDs, paging clamps, delete).
- Controller integration tests via `TestWebApplicationFactory` — happy path, validation errors, admin auth guard, and the full delete-then-404 round trip.

Integration tests use EF Core InMemory; see [CLAUDE.md](CLAUDE.md#testing-patterns-read-before-touching-testwebapplicationfactory) for the non-obvious bits (EF Core 10's `IDbContextOptionsConfiguration<TContext>` registration, why `HasData` is reseeded explicitly, and why FluentValidation's `EmailAddress()` needs careful test inputs).

## Architecture & design decisions

Captured in [CLAUDE.md](CLAUDE.md), in particular:

- The **Result<T>** pattern — services never throw for expected failures; controllers translate `Result` → `IActionResult`.
- **Aggregate root + value object** — `Subscription` is the aggregate; mutations only via `Create` / `UpdatePreferences`. `Email` is a value object with its own validation and normalisation (trim + lower-case).
- **`AppDbContext` doubles as `IUnitOfWork`** — the same scoped instance is exposed under both interfaces, so repositories share the context with the unit-of-work boundary the service controls.
- **Thin controllers** — no business logic; service call + status code translation.

## Security & privacy

The brief asks for senior-developer thinking, not a production system. What's in place:

- **Admin key auth** on management endpoints (`AdminKeyAuthFilter` checks `X-Admin-Key`).
- **Rate limiting** (fixed window, 10/min) on the public POST.
- **CORS** locked to an explicit allow-list (`Cors:AllowedOrigins`).
- **Global exception handler** returns a generic message — no stack traces leak.
- **HTTPS redirection** enabled.
- **Consent + timestamp** captured at creation; consent cannot be bypassed (`Subscription.Create` throws if `consentGiven` is false).
- **Email normalisation** prevents accidental duplicates from casing/whitespace.
- **No PII in logs** beyond what ASP.NET Core writes by default.
- Connection-string password is intentionally local-dev only and lives in `appsettings.json` to keep setup easy. For deployment it should move to a secret store / env var.

What I'd add for production: API-key hashing (current check is plain-string compare), structured audit logging on admin mutations, a soft-delete flag rather than hard delete, and a proper unsubscribe / data-export flow for GDPR.

## Assumptions

- "Upsert by email" is the right duplicate-handling behaviour — re-submitting the form with the same email replaces the previous preferences and updates the consent timestamp.
- Hard delete (rather than soft delete) is acceptable for the admin endpoint at this stage.
- A single shared admin key is sufficient for the management endpoints. A real system would have per-user admin auth.
- Lookup tables (subscriber types / communication preferences / newsletter interests) are stable enough to be seeded via EF migrations rather than managed through the API.
- The brief's "Other" subscriber type is enough of a catch-all — I added "Developer" as an extra option but kept the list short.

## AI usage

I used AI tooling throughout this exercise — primarily **Claude Code** (Anthropic) inside VS Code — and want to be explicit about how and where.

**What I used it for**

- Scaffolding the Clean Architecture project layout and project references.
- Drafting boilerplate: EF Core configurations, repository implementations, controller skeletons.
- Suggesting the FluentValidation rules and the conditional-validation layer inside `SubscriptionService`.
- Test scaffolding for both the unit tests and the `WebApplicationFactory`-based integration tests.
- Diagnosing two real .NET 10 / EF Core 10 issues that came up during testing (see below).

**What I changed, challenged, or rejected**

- *Validator strictness.* The first AI-suggested email validation relied only on FluentValidation's `EmailAddress()`. I tested it against obviously-broken inputs (`"@nodomain.com"`, `"missing@domain"`) and found it lenient, so I kept the value-object regex in `Email.Create` as the stricter line of defence and adjusted the validator tests to use inputs that actually fail (`"notanemail"`, etc.).
- *DbContext lifetimes.* Initial AI suggestion treated `IUnitOfWork` and `AppDbContext` as separate scoped registrations. That would have given different instances per resolution; I changed the registration to resolve `IUnitOfWork` from the same `AppDbContext` instance.
- *Test isolation.* AI initially suggested generating a fresh InMemory database name inside the `UseInMemoryDatabase(...)` lambda. That gave a different DB per scope and broke seeded data — I lifted the name to a readonly field on the factory.
- *Provider conflicts.* The AI's first take on swapping in InMemory only removed `DbContextOptions<AppDbContext>`. EF Core 10 also registers `IDbContextOptionsConfiguration<TContext>` as a Singleton, so the SqlServer provider was still active and the test host threw a dual-provider error. The fix is in [TestWebApplicationFactory](tests/NewsletterPreferences.Api.Tests/TestWebApplicationFactory.cs) — I now remove *every* descriptor whose service type has `AppDbContext` as a generic argument.
- *`Database.IsRelational()` in Program.cs.* Suggested as the test/prod branch — but it queries the EF internal service provider, which still sees both providers and throws. Switched to `app.Environment.IsEnvironment("Test")`.
- *Comments and docstrings.* I removed most of the AI-generated XML doc-comments — most just restated identifier names. The few remaining comments mark genuinely non-obvious behaviour (e.g. why explicit seeding is needed in the test factory).

**Risks I checked**

- *Correctness:* the test suite exercises the happy paths and every validation branch (60 tests, all passing).
- *Security:* admin key check, rate limit on the public POST, CORS allow-list, no stack traces leaked from the exception handler, HTTPS redirect.
- *Maintainability:* enforced layering through project references so Application/Domain can't accidentally take a dependency on EF Core.
- *Testing gaps:* repositories themselves aren't unit-tested in isolation — they're exercised via the integration tests against EF Core InMemory. Acceptable trade-off for an exercise; in production I'd add a thin set of repository tests against a real SQL Server in CI.

**What I'd improve with more time**

- Hash the admin key rather than plain-string compare; ideally swap for real authentication (JWT or per-user keys).
- Audit logging on admin mutations.
- Soft delete + a proper GDPR unsubscribe / data-export endpoint.
- A repository-level test suite running against a real SQL Server (Testcontainers) in CI.
- Structured logging (Serilog) with correlation IDs and a /health endpoint.
- OpenAPI client generation for the React app.