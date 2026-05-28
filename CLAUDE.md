# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project context

Newsletter Preference System backend — a recruitment exercise for Aspire Software / SDS. Stack: **.NET 10 + EF Core 10 + SQL Server**. A React + TypeScript frontend (Phase 3) is planned but not yet started.

Solution file is the new-format `NPS.slnx` (XML, no GUIDs). Pass it explicitly to `dotnet` commands.

## Commands

```powershell
# Build / restore
dotnet build NPS.slnx
dotnet restore NPS.slnx

# Run the API (Swagger at /swagger in Development)
dotnet run --project NewsletterPreferences.Api

# Run all tests
dotnet test NPS.slnx

# Run a single test project
dotnet test tests/NewsletterPreferences.Application.Tests
dotnet test tests/NewsletterPreferences.Api.Tests

# Run a single test by name
dotnet test NPS.slnx --filter "FullyQualifiedName~AdminControllerTests.GetPaged_WithoutBearer_Returns401"

# EF Core migrations (Infrastructure owns the model, Api is the startup project)
dotnet ef migrations add <Name> --project NewsletterPreferences.Infrastructure --startup-project NewsletterPreferences.Api
dotnet ef database update --project NewsletterPreferences.Infrastructure --startup-project NewsletterPreferences.Api
```

When iterating on tests, **do not pass `--no-build`** — it has caused stale-binary confusion (old test cases lingering in compiled DLLs).

## Architecture

Clean Architecture with project references enforcing the dependency direction:

```
Domain  ←  Application  ←  Infrastructure
                ↑                ↑
                └──── Api ───────┘
```

- **Domain** — no dependencies. Entities (`Subscription` is the aggregate root), value object (`Email`), repository/UoW interfaces. Entities have private setters and static `Create` factory methods; mutations go through methods like `Subscription.UpdatePreferences(...)`. Domain enforces invariants (e.g. `Create` throws if `consentGiven` is false).
- **Application** — depends only on Domain. Services return `Result<T>` / `Result` (success + value, or error string + validation-error list). FluentValidation validators run inside the service before any DB work. `SubscriberType`, `CommunicationPreference`, `NewsletterInterest` are lookup tables exposed via `ILookupRepository`.
- **Infrastructure** — EF Core, `AppDbContext` (which also **implements `IUnitOfWork`** — they are registered to the same instance), repository implementations, migrations. SQL Server provider only.
- **Api** — controllers, WebAuthn admin auth (`AdminAuthController` + `ConfigureJwtBearerOptions`), `GlobalExceptionHandler` (returns sanitized 500), rate limiting (`"public"` policy on the public POST endpoint).

### Endpoint surface

- `POST /api/subscriptions` — public, rate-limited, upsert by email. Returns `201 CreatedAtAction` for new, `200 Ok` for update (`IsUpdate` flag on the response).
- `GET /api/admin/subscriptions` (+ `/{id}`, `DELETE /{id}`) — admin-only, gated by `[Authorize(Policy = "AdminOnly")]` which requires a JWT with the `Admin` role.
- `GET /api/admin/auth/status` — public, reports whether the admin has registered any passkey (drives the FE's enroll-vs-sign-in branch).
- `POST /api/admin/auth/register/{begin,complete}` — WebAuthn enrollment ceremony. Anonymous *only* while the admin has zero credentials (bootstrap); afterwards requires an authenticated JWT.
- `POST /api/admin/auth/login/{begin,complete}` — WebAuthn assertion ceremony. Returns a JWT on success.
- `GET /api/lookups` — public lookup data for the frontend dropdowns.

### Admin auth flow

Admin auth uses **WebAuthn / Passkey** (no passwords or API keys). The `AdminAuthService` orchestrates the four ceremony halves via `Fido2NetLib`, stashing per-ceremony challenges in `IMemoryCache` keyed by an opaque token echoed back to the client. On successful assertion, `JwtTokenService` issues a short-lived HMAC-SHA256 JWT with `role = Admin` that the FE attaches as `Authorization: Bearer ...` to every admin request.

`ConfigureJwtBearerOptions` (`Api/Authentication/`) configures the bearer middleware lazily via `IConfigureNamedOptions<JwtBearerOptions>` so it picks up integration-test config overrides — do NOT inline JWT config in `Program.cs`'s `AddJwtBearer(...)` callback or the test factory's signing key won't apply.

### Two-layer validation

Don't conflate them:

1. **`UpsertSubscriptionRequestValidator`** (FluentValidation) — shape-level rules: required, max length, email format, non-empty lists, consent must be true.
2. **Conditional/referential checks inside `SubscriptionService.UpsertAsync`** — checked *after* the validator passes. These include: PHONE or SMS preference requires a phone number; POST preference requires a postal address; subscriber type / pref IDs / interest IDs must exist in the lookup tables. All produce `Result.ValidationFailure([...])`, which the controller surfaces as `400 BadRequest`.

The `Email` value object has its own stricter regex check (`Email.Create` throws `ArgumentException`); the service catches that and converts it to a validation failure.

### Configuration

- `appsettings.json` keys: `ConnectionStrings:DefaultConnection`, `AdminAuth:Username`/`DisplayName`, `Jwt:SigningKey`/`Issuer`/`Audience`/`ExpiryMinutes`, `Fido2:ServerDomain`/`Origins`, `Cors:AllowedOrigins`. The committed JWT signing key is a placeholder — replace before deploying. The seeded admin user has no credentials by default; first-time enrollment via the FE bootstraps the first passkey.
- The Api branches DB initialisation on environment: `Test` → `EnsureCreatedAsync`, otherwise → `MigrateAsync`. This branch is load-bearing for the integration tests (see below); don't switch it back to `Database.IsRelational()` — that throws during test host startup because the EF internal service provider sees both SqlServer and InMemory registered.

## Testing patterns (read before touching `TestWebApplicationFactory`)

`tests/NewsletterPreferences.Application.Tests/` is pure unit tests (xUnit + Moq + FluentAssertions). `tests/NewsletterPreferences.Api.Tests/` uses `WebApplicationFactory<Program>` against EF Core InMemory.

Several non-obvious things to know:

- **Swapping DbContext provider in the factory requires removing more than `DbContextOptions<AppDbContext>`.** EF Core 10 also registers `IDbContextOptionsConfiguration<AppDbContext>` (Singleton) plus other open-generic services. `TestWebApplicationFactory.IsAppDbContextRelated` removes *every* descriptor whose service type has `AppDbContext` as its single generic type argument. If you add a new EF Core extension that registers another open generic, this filter should already catch it — but verify.
- **InMemory database name must be captured once per factory.** `_dbName` is a readonly field — putting `Guid.NewGuid()` inside the `UseInMemoryDatabase(...)` lambda gives a different DB per scope and tests fail to find seeded data.
- **`HasData` seeding is unreliable with the InMemory provider across scopes.** `TestWebApplicationFactory.EnsureDatabaseCreatedAsync()` explicitly seeds the lookup tables (SubscriberTypes, CommunicationPreferences, NewsletterInterests). Call it from any test that expects lookups to exist. Don't rely on `EnsureCreatedAsync` alone.
- **`AdminControllerTests` seeds subscriptions by POSTing to the API**, not by writing to `AppDbContext` directly. This is deliberate — direct-DbContext seeding ran into the same scope/seed issues.
- **`UpsertSubscriptionRequest` is a class with `init` setters, not a record.** `with` expressions don't compile. The test helper `ValidRequest(firstName: ..., email: ...)` returns explicit `new()` instances — keep that pattern when adding tests.
- **FluentValidation 12.1.1 `.EmailAddress()` is very lenient by default.** Strings like `"missing@domain"`, `"@nodomain.com"`, `"spaces in@email.com"` all pass. Reliable invalid-email test cases are strings with no `@` at all (`"notanemail"`, `"plainaddress"`, `"no-at-sign-here"`). The stricter regex lives in `Email.Create`, which the service invokes after FluentValidation passes.
- **Setting EF navigation properties for unit tests** requires reflection because they have private setters — see `BuildSubscriptionWithNavigations` in `SubscriptionServiceTests.cs` for the pattern (`GetProperty(...).GetSetMethod(nonPublic: true).Invoke(...)`).
- **The validator class is `public`**, not `internal`, specifically so it can be instantiated directly in the validator tests. Keep it public.

## Conventions worth preserving

- Don't bypass the aggregate root: `Subscription` mutations go through `Create` / `UpdatePreferences`, not by setting properties.
- Don't leak EF types or `DbContext` into `Application` or `Domain` — those projects must not reference `Microsoft.EntityFrameworkCore`.
- Controllers stay thin: call a service, translate `Result` → `IActionResult`. No business logic in controllers.
- Use the `Result` pattern for service outcomes; reserve exceptions for genuinely exceptional cases (`GlobalExceptionHandler` catches anything that escapes).
