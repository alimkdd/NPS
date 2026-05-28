# Newsletter Preference System

A small full-stack application for capturing, validating, persisting, and managing newsletter subscription preferences. Submitted as part of the Full Stack Developer recruitment exercise for SDS / Aspire Software.

> **This is the backend.** The React + TypeScript frontend lives in a sibling repo: [NPS-Frontend](https://github.com/alimkdd/NPS-Frontend) (default origin: `https://localhost:5173`, already on the CORS allow-list here).

---

## Tech stack

| Layer        | Choice                                                                              |
| ------------ | ----------------------------------------------------------------------------------- |
| API          | ASP.NET Core 10, controllers, FluentValidation 12                                   |
| Auth         | **WebAuthn / FIDO2** (Fido2NetLib 4) for admin sign-in, **JWT bearer** for sessions |
| Persistence  | EF Core 10 + SQL Server                                                             |
| Architecture | Clean Architecture — Domain / Application / Infrastructure / Api                    |
| Logging      | Serilog (compact JSON, daily rolling file, correlation IDs)                         |
| Testing      | xUnit, Moq, FluentAssertions, `WebApplicationFactory<Program>`, EF InMemory         |
| Local infra  | Docker Compose (SQL Server 2022)                                                    |

Solution file is the new XML-format [NPS.slnx](NPS.slnx).

## Solution structure

```
NewsletterPreferences.Domain          ← entities, value objects, interfaces — no external deps
NewsletterPreferences.Application     ← services, DTOs, validators, FIDO2 ceremony orchestration
NewsletterPreferences.Infrastructure  ← EF Core, repositories, migrations, AppDbContext
NewsletterPreferences.Api             ← controllers, JWT bearer + WebAuthn auth wiring, middleware
tests/
  NewsletterPreferences.Application.Tests   ← unit tests (38)
  NewsletterPreferences.Api.Tests           ← integration tests via TestWebApplicationFactory (22)
```

Project references enforce the dependency direction: Api depends on Application + Infrastructure; Infrastructure depends on Application + Domain; Application depends only on Domain.

A deeper architectural note for contributors is in [CLAUDE.md](CLAUDE.md).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the SQL Server container)
- One-time: `dotnet dev-certs https --trust` (ASP.NET dev certificate, needed for the https launch profile)

## Quick start

```powershell
# 1. Copy the env file and (optionally) change the password
copy .env.example .env

# 2. Start SQL Server
docker compose up -d

# 3. Apply EF Core migrations (includes the AdminUsers + WebAuthnCredentials tables)
dotnet ef database update --project NewsletterPreferences.Infrastructure --startup-project NewsletterPreferences.Api

# 4. Run the API on the HTTPS profile (binds both https://localhost:7287 and http://localhost:5289)
dotnet run --project NewsletterPreferences.Api --launch-profile https
```

Swagger UI is at `https://localhost:7287/swagger` in Development.

The startup also **seeds the admin user** (`admin` / `Administrator`) if missing. The admin row has no credentials yet — the first WebAuthn enrollment from the frontend is anonymous-allowed exactly once, then the slot is closed.

If you change `SA_PASSWORD` in `.env`, update the matching password in [NewsletterPreferences.Api/appsettings.json](NewsletterPreferences.Api/appsettings.json) (`ConnectionStrings:DefaultConnection`) so the API can connect.

## Configuration

All in [appsettings.json](NewsletterPreferences.Api/appsettings.json):

| Key                                   | Purpose                                                                                                                          |
| ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string                                                                                                     |
| `AdminAuth:Username` / `DisplayName`  | Identity of the seeded admin user. The first WebAuthn enrollment binds a passkey to this user.                                   |
| `Jwt:Issuer` / `Audience`             | Standard JWT validation claims. Unique GUID-suffixed values are committed; replace if you want them publicly meaningful.         |
| `Jwt:SigningKey`                      | 64-byte (base64) HMAC-SHA256 signing key. **In production move to user-secrets / Azure Key Vault.**                              |
| `Jwt:ExpiryMinutes`                   | Token TTL. Default 60.                                                                                                           |
| `Fido2:ServerDomain` / `ServerName`   | RP ID (`localhost` for dev) and RP display name. **Origin matching is strict** — the RP ID must be the bare hostname.            |
| `Fido2:Origins`                       | Array of origins the FIDO2 verifier accepts. Default: `https://localhost:5173`. WebAuthn requires HTTPS (or `http://localhost`). |
| `Cors:AllowedOrigins`                 | Array of frontend origins allowed by CORS                                                                                        |
| `DataProtection:KeyPath`              | Directory used by ASP.NET Data Protection for its key ring (gitignored). Defaults to `keys/` next to the binary.                 |
| `Serilog:*`                           | Serilog sink + level configuration                                                                                               |

## API surface

### Public

| Method | Route                            | Notes                                                                                                                                                                                              |
| ------ | -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `POST` | `/api/subscriptions`             | Upsert by email. Returns `201 CreatedAtAction` for a new row, `200 OK` for an update (`IsUpdate` flag on the response). Rate-limited (10/min/IP).                                                  |
| `POST` | `/api/subscriptions/unsubscribe` | Soft-deletes by email. Always returns `202 Accepted` with the same body regardless of whether the email existed (no enumeration leak). Rate-limited.                                               |
| `GET`  | `/api/lookups`                   | Returns subscriber types, communication preferences, and newsletter interests for frontend dropdowns.                                                                                              |

### Admin authentication — WebAuthn ceremony

| Method | Route                              | Notes                                                                                                                |
| ------ | ---------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `GET`  | `/api/admin/auth/status`           | Public. `{ hasRegisteredCredentials, username, displayName }`. The FE uses this to switch between enroll and sign-in.|
| `POST` | `/api/admin/auth/register/begin`   | Anonymous **only** while the admin has zero credentials. Returns `CredentialCreateOptions` + an opaque `challengeToken`.|
| `POST` | `/api/admin/auth/register/complete`| Verifies the attestation, stores the public key + credential id, closes anonymous enrollment.                        |
| `POST` | `/api/admin/auth/login/begin`      | Returns `AssertionOptions` + a `challengeToken` for the assertion ceremony.                                          |
| `POST` | `/api/admin/auth/login/complete`   | Verifies the assertion and issues a JWT (`AccessToken`, `ExpiresAtUtc`, `DisplayName`).                              |

Challenges are stored in `IMemoryCache` keyed by the opaque token, TTL 5 minutes. The JWT carries `role = Admin` and is required as `Authorization: Bearer <jwt>` on every admin endpoint.

### Admin (Bearer JWT, `Authorize(Policy = "AdminOnly")`)

| Method   | Route                            | Notes                                                                                                                                          |
| -------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET`    | `/api/admin/subscriptions`       | Paged list. Query: `searchTerm`, `subscriberTypeId`, `communicationPreferenceId`, `interestId`, `page`, `pageSize` (clamped 1–100).            |
| `GET`    | `/api/admin/subscriptions/stats` | `{ totalActive, newLast7Days, newLast30Days }` for the dashboard cards.                                                                        |
| `GET`    | `/api/admin/subscriptions/{id}`  | Get a single subscription.                                                                                                                     |
| `DELETE` | `/api/admin/subscriptions/{id}`  | **Soft delete** — sets `IsDeleted = true` + `DeletedAt`. A global query filter hides the row from subsequent reads; a filtered unique index lets the same email re-subscribe afterwards. |

Without a valid bearer token admin endpoints return `401`. Every admin request is recorded in the `AdminAuditLogs` table (action, target id, correlation id, client IP, status code). Admin endpoints are rate-limited at 30/min/IP.

## Validation

Two layers — both server-side:

1. **`UpsertSubscriptionRequestValidator`** (FluentValidation) — required fields, max lengths, email format, non-empty preference/interest lists, consent must be `true`.
2. **Conditional + referential checks inside `SubscriptionService.UpsertAsync`** — runs only if (1) passes:
   - Selecting *Phone* or *SMS* requires a phone number.
   - Selecting *Post* requires a postal address.
   - All submitted lookup IDs must exist in the database.

The `Email` value object additionally enforces a stricter regex via `Email.Create`; failures surface as validation errors, not exceptions.

Client-side validation lives in the [FE repo](https://github.com/alimkdd/NPS-Frontend) and is a strict superset of these rules.

## Testing

```powershell
# Run everything
dotnet test NPS.slnx

# One project at a time
dotnet test tests/NewsletterPreferences.Application.Tests
dotnet test tests/NewsletterPreferences.Api.Tests

# A single test
dotnet test NPS.slnx --filter "FullyQualifiedName~AdminControllerTests.GetPaged_WithoutBearer_Returns401"
```

**60 tests pass** (38 unit + 22 integration). Coverage includes:

- Validator rules (empty / over-length / invalid email / missing consent).
- `SubscriptionService` upsert flow (new + duplicate-email update, conditional PHONE/SMS/POST rules, invalid lookup IDs, paging clamps, delete).
- Controller integration tests via `TestWebApplicationFactory` — happy path, validation errors, admin auth guard (no bearer + invalid bearer + valid bearer), and the full delete-then-404 round trip.

Integration tests issue real JWTs via the live `JwtTokenService` so the bearer middleware is exercised end-to-end. See [CLAUDE.md](CLAUDE.md#testing-patterns-read-before-touching-testwebapplicationfactory) for the non-obvious bits (EF Core 10's `IDbContextOptionsConfiguration<TContext>` registration, why `HasData` is reseeded explicitly, and why JWT bearer options are configured via `IConfigureNamedOptions` rather than inline in `Program.cs`).

## Architecture & design decisions

Captured in [CLAUDE.md](CLAUDE.md), highlights:

- **Result<T> pattern** — services never throw for expected failures; controllers translate `Result` → `IActionResult`.
- **Aggregate root + value object** — `Subscription` is the aggregate; mutations only via `Create` / `UpdatePreferences`. `Email` is a value object with its own validation and normalisation (trim + lower-case).
- **`AppDbContext` doubles as `IUnitOfWork`** — the same scoped instance is exposed under both interfaces, so repositories share the context with the unit-of-work boundary the service controls.
- **Thin controllers** — no business logic; service call + status code translation.
- **Lazy JwtBearerOptions binding** — configured via `IConfigureNamedOptions<JwtBearerOptions>` (`Api/Authentication/ConfigureJwtBearerOptions.cs`) so integration-test config overrides actually apply. Inlining the JWT config in `Program.cs`'s `AddJwtBearer(...)` callback binds too early and the test signing key never reaches the validator.

## Security & privacy

A self-audit and four hardening passes have been applied. What's in place:

**AuthN / AuthZ**
- **WebAuthn / passkey** admin sign-in (Windows Hello, Touch ID, Face ID, or USB security key). The biometric never leaves the device; the server only ever sees the public half of the credential and verifies signed challenges.
- Short-lived **JWT** (HMAC-SHA256, 60 min default) issued after successful assertion.
- Bootstrap rule: anonymous registration is allowed only while the admin has zero credentials. After the first passkey is enrolled, the slot closes — registering more devices requires already being signed in.

**Transport / headers**
- HTTPS redirection.
- HSTS (`max-age=365 days; includeSubDomains; preload`) outside Dev/Test.
- Response headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, restrictive `Permissions-Policy`. `Server` / `X-Powered-By` stripped.
- CORS locked to a configured allow-list with explicit methods (`GET, POST, DELETE, OPTIONS`) and an explicit header allow-list (`Content-Type, Authorization, X-Correlation-Id`).

**Rate limiting**
- `Microsoft.AspNetCore.RateLimiting` partitioned by `RemoteIpAddress` (not a single global bucket). `public` policy: 10/min/IP; `admin` policy: 30/min/IP. Rejection returns `429`.

**Input safety**
- Two-layer validation (FluentValidation + service-layer referential checks) + `Email` value-object regex.
- Kestrel `MaxRequestBodySize = 16 KB` (form is tiny; cap prevents DoS via large bodies).
- `JsonSerializerOptions.MaxDepth = 8`.

**Data protection**
- `PhoneNumber` and `PostalAddress` encrypted at rest with ASP.NET Core Data Protection (`IDataProtector` value converter on the EF Core column). Keys persisted under `keys/` (gitignored) — for production, swap the persistence to Azure Key Vault / DPAPI / certificate.
- `Email` stays plaintext because it's the upsert lookup; for production use SQL Server TDE or Always Encrypted on the column.

**Privacy / GDPR**
- `Subscription.MarkAsDeleted()` + global query filter — admin DELETE is a **soft delete**, not a row removal. Filtered unique index on `Email` lets a fresh re-subscribe after unsubscribe succeed without conflicting with the dead row.
- Public `POST /api/subscriptions/unsubscribe` lets the subscriber remove themselves by email. Always returns `202 Accepted` with the same body whether the email existed or not — no enumeration leak.
- Consent + timestamp captured at creation; cannot be bypassed (`Subscription.Create` throws if `consentGiven` is false). Re-subscribing refreshes the consent timestamp.
- Email normalisation (trim + lower) prevents accidental duplicates from casing/whitespace.

**Observability without leaking PII**
- Structured logging via Serilog (compact JSON, daily rolling file, 14-day retention).
- Correlation ID per request (incoming `X-Correlation-Id` honoured, else minted) — echoed on every response, including 500s, and added to every log line via a logger scope.
- `GlobalExceptionHandler` writes a rich server log (correlationId, timestamp, method, path, redacted query string, exception type + message, inner type + message). Response body is `{ error, correlationId, timestamp }`; in Dev only it additionally carries the exception details. `DbUpdateException` messages are sanitised before logging — they can contain row values.
- Sensitive query keys (`searchTerm`, `email`, `phone`, `token`, etc.) are redacted in the request-log query string.
- `AdminAuditLogs` row written for every admin call: action, target id, correlation id, client IP, status code.

**What's still future work** (intentionally not done):
- Per-credential management UI (list / rename / revoke registered passkeys).
- Refresh tokens or longer-lived sessions with sliding expiry. Currently the admin re-authenticates after 60 minutes.
- Replay the JWT signing key out of `appsettings.json` into user-secrets / Azure Key Vault.
- Data-export endpoint (`GET /me?token=…`) for GDPR subject-access requests.
- `dotnet list package --vulnerable --include-transitive` in CI; fail on High/Critical.
- Repository-level test suite against a real SQL Server (Testcontainers).

## Assumptions

- "Upsert by email" is the right duplicate-handling behaviour — re-submitting the form with the same email replaces the previous preferences and updates the consent timestamp.
- Soft delete (not hard delete) is appropriate so consent revocation is auditable.
- A single admin user (`admin`) is enough for this exercise. A real system would have per-user admin auth — the schema (`AdminUsers` table, FK to credentials) already supports multiple users; only the seeding logic is single-user.
- WebAuthn / passkey is the right answer to "protected endpoint" for a small system that already runs over HTTPS. Trade-off: the reviewer needs a platform authenticator (Windows Hello, Touch ID, or a security key) on their own machine to log in. Documented in the FE README.
- Lookup tables (subscriber types / communication preferences / newsletter interests) are stable enough to be seeded via EF migrations rather than managed through the API.
- The brief's "Other" subscriber type catches anything else; I added "Developer" as an extra option but kept the list short.

## For the reviewer — how to demo

The admin sign-in uses biometrics. To exercise the full flow on your machine:

1. Run the BE on the **https profile** (`dotnet run --project NewsletterPreferences.Api --launch-profile https`) and the FE over `https://localhost:5173`.
2. Visit `/admin`. Because no passkey is registered yet, you'll see the **first-time enrollment** screen.
3. Click **Enroll this device** — your browser will prompt for Windows Hello, Touch ID, or a security key. After the prompt, you're auto-signed-in.
4. Subsequent visits show the **sign-in** screen instead — one click + biometric prompt and you're in.

If you don't have a platform authenticator, you can either plug in a USB security key (e.g. YubiKey) or skip the live demo of admin sign-in and discuss the architecture during the walkthrough. The schema, ceremony orchestration, and JWT issuance are all exercised by the integration tests so you can also point at those.

## AI usage

I used AI tooling throughout this exercise — primarily **Claude Code** (Anthropic) inside VS Code — and want to be explicit about how and where.

**What I used it for**

- Scaffolding the Clean Architecture project layout and project references.
- Drafting boilerplate: EF Core configurations, repository implementations, controller skeletons.
- Suggesting the FluentValidation rules and the conditional-validation layer inside `SubscriptionService`.
- Test scaffolding for both the unit tests and the `WebApplicationFactory`-based integration tests.
- Designing the WebAuthn ceremony orchestration on top of Fido2NetLib and the JWT bearer wiring.
- Diagnosing real .NET 10 / EF Core 10 issues that came up during testing and the WebAuthn migration (see below).

**What I changed, challenged, or rejected**

- *Validator strictness.* The first AI-suggested email validation relied only on FluentValidation's `EmailAddress()`. I tested it against obviously-broken inputs (`"@nodomain.com"`, `"missing@domain"`) and found it lenient, so I kept the value-object regex in `Email.Create` as the stricter line of defence and adjusted the validator tests to use inputs that actually fail.
- *DbContext lifetimes.* Initial AI suggestion treated `IUnitOfWork` and `AppDbContext` as separate scoped registrations. That would have given different instances per resolution; I changed the registration to resolve `IUnitOfWork` from the same `AppDbContext` instance.
- *Test isolation.* AI initially suggested generating a fresh InMemory database name inside the `UseInMemoryDatabase(...)` lambda. That gave a different DB per scope and broke seeded data — lifted the name to a readonly field on the factory.
- *Provider conflicts.* Removing only `DbContextOptions<AppDbContext>` wasn't enough — EF Core 10 also registers `IDbContextOptionsConfiguration<TContext>` as a Singleton, so the SqlServer provider was still active. The factory now removes every descriptor whose service type has `AppDbContext` as a generic argument.
- *`Database.IsRelational()` in Program.cs.* Suggested as the test/prod branch — but it queries the EF internal service provider, which still sees both providers and throws. Switched to `app.Environment.IsEnvironment("Test")`.
- *Inline `AddJwtBearer` configuration.* The first attempt configured `TokenValidationParameters` inline in `Program.cs`. That bound the JWT settings *before* the test factory's `ConfigureAppConfiguration` overrides applied, so the bearer middleware kept the placeholder signing key and rejected real test JWTs. Moved configuration into `ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>` so binding happens lazily after DI is fully built.
- *EF tracking for the new passkey row.* The initial WebAuthn registration code called `_repo.Update(admin)` after mutating the aggregate. That walked the navigation graph and marked the newly created `WebAuthnCredential` as Modified (because `Entity.Id` is pre-assigned via `Guid.NewGuid()` at construction), so SaveChanges emitted UPDATE against a non-existent row → `DbUpdateConcurrencyException`. Fix: explicitly attach via `context.WebAuthnCredentials.AddAsync` so the state is unambiguously Added.
- *AI suggestion to keep using the API key during the WebAuthn migration as "fallback".* Rejected — it would have left an obvious downgrade attack surface. Replaced with a one-time bootstrap rule: anonymous registration only allowed while zero credentials exist.

**Risks I checked**

- *Correctness:* the test suite exercises the happy paths and every validation branch (60 tests, all passing).
- *Security:* WebAuthn replay/origin-binding handled by Fido2NetLib, JWT signing key length validated, CORS allow-list, no stack traces leaked from the exception handler outside Dev, HTTPS redirect.
- *Maintainability:* enforced layering through project references so Application/Domain can't accidentally take a dependency on EF Core.
- *Testing gaps:* the WebAuthn endpoints themselves aren't unit-tested (mocking `IFido2` end-to-end is brittle and adds little); the bearer auth + admin guard *is* tested via integration tests that issue real JWTs through `JwtTokenService`. The repositories are exercised via integration tests against EF Core InMemory.

**What I'd improve with more time**

- Lift the JWT signing key out of `appsettings.json` into user-secrets / Azure Key Vault.
- Per-credential admin UI (list / rename / revoke registered passkeys).
- Repository-level test suite running against a real SQL Server (Testcontainers) in CI.
- OpenAPI client generation for the React app to keep DTOs in sync automatically.
- Refresh tokens or sliding-expiry sessions so the admin doesn't re-authenticate every 60 minutes.
