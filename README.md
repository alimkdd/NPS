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

| Key                                    | Purpose                                                          |
| -------------------------------------- | ---------------------------------------------------------------- |
| `ConnectionStrings:DefaultConnection`  | SQL Server connection string                                     |
| `AdminSettings:ApiKeyHash`             | Hex-encoded SHA-256 of the admin key; compared in constant time. |
| `Cors:AllowedOrigins`                  | Array of frontend origins allowed by CORS                        |
| `DataProtection:KeyPath`               | Directory used by ASP.NET Data Protection for its key ring (gitignored). Defaults to `keys/` next to the binary. |
| `Serilog:*`                            | Serilog sink + level configuration.                              |

The committed `ApiKeyHash` is the hash of the dev placeholder `nps-dev-admin-key-do-not-use-in-prod`. For any non-local deployment, override it via env var (`AdminSettings__ApiKeyHash=...`) — see the "Security & privacy" section below for the rotation snippet.

## API surface

### Public

| Method | Route                              | Notes                                                   |
| ------ | ---------------------------------- | ------------------------------------------------------- |
| `POST` | `/api/subscriptions`               | Upsert by email. Always returns `202 Accepted` with `{ subscriptionId, correlationId, timestamp }` (identical shape for new + existing — no email-enumeration leak). Rate-limited (10/min/IP). |
| `POST` | `/api/subscriptions/unsubscribe`   | Soft-deletes by email. Always returns `202 Accepted` with the same shape regardless of whether the email existed (no enumeration leak). Rate-limited (10/min/IP). |
| `GET`  | `/api/lookups`                     | Returns subscriber types, communication preferences, and newsletter interests for populating frontend dropdowns. |

### Admin (header `X-Admin-Key: <key>`)

| Method   | Route                                | Notes                                              |
| -------- | ------------------------------------ | -------------------------------------------------- |
| `GET`    | `/api/admin/subscriptions`           | Paged list. Query: `searchTerm`, `subscriberTypeId`, `communicationPreferenceId`, `interestId`, `page`, `pageSize` (clamped 1–100). |
| `GET`    | `/api/admin/subscriptions/{id}`      | Get a single subscription.                         |
| `DELETE` | `/api/admin/subscriptions/{id}`      | **Soft delete** — sets `IsDeleted = true` + `DeletedAt`. Subsequent reads hide the row via a global query filter. |

Without (or with a wrong) `X-Admin-Key`, admin endpoints return `401`. Every admin request is recorded in the `AdminAuditLogs` table (action, target id, correlation id, client IP, status code). Admin endpoints are rate-limited at 30/min/IP.

The admin key is stored as a **SHA-256 hash** (`AdminSettings:ApiKeyHash`, hex-encoded) and compared in constant time. The default dev value hashes the key `nps-dev-admin-key-do-not-use-in-prod`. To rotate:

```powershell
# Pick a strong random key and compute its hash
$key = "<your-new-key>"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($key)
-join ([System.Security.Cryptography.SHA256]::HashData($bytes) | ForEach-Object { $_.ToString('x2') })
```

Put the resulting hex in `AdminSettings:ApiKeyHash` (env var `AdminSettings__ApiKeyHash` in deployment). Keep the plaintext key secret — only the hash should ever live on disk.

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

A self-audit and four hardening passes have been applied beyond the brief's defaults. What's in place:

**AuthN / AuthZ**
- Admin key stored as **SHA-256 hash** (`AdminSettings:ApiKeyHash`) and compared with `CryptographicOperations.FixedTimeEquals` — constant-time, no plaintext key on disk.
- Admin auth filter, rate limit (30/min/IP), and audit-log filter applied to every admin endpoint via `[ServiceFilter]` chain.

**Transport / headers**
- HTTPS redirection.
- HSTS (`max-age=365 days; includeSubDomains; preload`) outside Dev/Test.
- Response headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, restrictive `Permissions-Policy`. `Server` / `X-Powered-By` stripped.
- CORS locked to a configured allow-list with explicit methods (`GET, POST, DELETE, OPTIONS`) and an explicit header allow-list.

**Rate limiting**
- `Microsoft.AspNetCore.RateLimiting` partitioned by `RemoteIpAddress` (not a single global bucket). `public` policy: 10/min/IP; `admin` policy: 30/min/IP. Rejection returns `429`.

**Input safety**
- Two-layer validation (FluentValidation + service-layer referential checks) + `Email` value-object regex.
- Kestrel `MaxRequestBodySize = 16 KB` (form is tiny; cap prevents DoS via large bodies).
- `JsonSerializerOptions.MaxDepth = 8`.

**Data protection**
- `PhoneNumber` and `PostalAddress` encrypted at rest with ASP.NET Core Data Protection (`IDataProtector` value converter on the EF Core column). Keys persisted under `keys/` (gitignored) — for production, swap the persistence to Azure Key Vault / DPAPI / certificate.
- `Email` stays plaintext because it's the upsert lookup; **for production use SQL Server TDE or Always Encrypted on the column**. Documented as the production path; current value-converter pattern shows the in-app encryption mechanism for the two free-text PII fields.

**Privacy / GDPR**
- `Subscription.MarkAsDeleted()` + global query filter — admin DELETE is a **soft delete**, not a row removal. Filtered unique index on `Email` lets a fresh re-subscribe after unsubscribe succeed without conflicting with the dead row.
- Public `POST /api/subscriptions/unsubscribe` lets the subscriber remove themselves by email. Always returns `202 Accepted` with the same body whether the email existed or not — no enumeration leak.
- `POST /api/subscriptions` also always returns `202 Accepted` (same body shape for new and existing).
- Consent + timestamp captured at creation; cannot be bypassed (`Subscription.Create` throws if `consentGiven` is false). Re-subscribing refreshes the consent timestamp.
- Email normalisation (trim + lower) prevents accidental duplicates from casing/whitespace.

**Observability without leaking PII**
- Structured logging via Serilog (compact JSON, daily rolling file, 14-day retention).
- Correlation ID per request (incoming `X-Correlation-Id` honoured, else minted) — echoed on every response, including 500s, and added to every log line via a logger scope.
- `GlobalExceptionHandler` writes a rich server log (correlationId, timestamp, method, path, redacted query string, exception type + message, inner type + message). Response body is `{ error, correlationId, timestamp }`; in Dev only it additionally carries the exception details. `DbUpdateException` messages are sanitised before logging — they can contain row values.
- Sensitive query keys (`searchTerm`, `email`, `phone`, `token`, etc.) are redacted in the request-log query string.
- `AdminAuditLogs` row written for every admin call: action, target id, correlation id, client IP, status code.

**What's still future work** (intentionally not done):
- Replace the single shared admin key with a proper auth scheme (JWT or per-user API keys).
- Token-mediated unsubscribe (subscriber would receive a signed token by email to confirm); the current endpoint is email-only for the exercise.
- Data-export endpoint (`GET /me?token=…`) for GDPR subject-access requests.
- `dotnet list package --vulnerable --include-transitive` in CI; fail on High/Critical.
- Repository-level test suite against a real SQL Server (Testcontainers).

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