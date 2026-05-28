# Newsletter Preference System — Quick Start

This is the short version. The full README in each repo has the same steps plus all the design / security / testing context.

- **Backend:** https://github.com/alimkdd/NPS
- **Frontend:** https://github.com/alimkdd/NPS-Frontend

---

## 1. Prerequisites

Install once:

| Tool | Why |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Backend runtime |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | SQL Server container |
| [Node.js 20+](https://nodejs.org/) | Frontend toolchain |
| [mkcert](https://github.com/FiloSottile/mkcert) (`winget install FiloSottile.mkcert`) | Trusted local HTTPS cert for the frontend |

One-time after install:
```powershell
dotnet dev-certs https --trust      # ASP.NET dev certificate
mkcert -install                     # mkcert local Certificate Authority
```

---

## 2. Backend (`NPS`)

```powershell
git clone https://github.com/alimkdd/NPS.git
cd NPS

# Start SQL Server
copy .env.example .env
docker compose up -d

# Apply migrations
dotnet ef database update --project NewsletterPreferences.Infrastructure --startup-project NewsletterPreferences.Api

# Run the API on the HTTPS profile
dotnet run --project NewsletterPreferences.Api --launch-profile https
```

The API now listens on:
- `https://localhost:7287` (primary, used by the FE)
- `http://localhost:5289` (fallback)
- Swagger at `https://localhost:7287/swagger`

---

## 3. Frontend (`NPS-Frontend`)

In a **separate terminal**:

```powershell
git clone https://github.com/alimkdd/NPS-Frontend.git
cd NPS-Frontend

# Generate the mkcert cert for the Vite dev server (one-time)
New-Item -ItemType Directory -Path certs -Force | Out-Null
cd certs
mkcert -cert-file localhost.pem -key-file localhost-key.pem localhost 127.0.0.1 ::1
cd ..

copy .env.example .env.local
npm install
npm run dev
```

The app is now at **https://localhost:5173**.

---

## 4. Try the app

| Route | What it does |
| --- | --- |
| `/` | Public newsletter subscription form |
| `/unsubscribe` | Public unsubscribe by email |
| `/admin` | Admin dashboard (biometric sign-in — see note below) |

The form's progress stepper lights up as you fill it in. Re-submitting with the same email updates the previous subscription (upsert by email).

### Admin sign-in (WebAuthn / passkey)

The admin page uses **biometric authentication** — Windows Hello, Touch ID, Face ID, or a USB security key. The biometric never leaves your device; the server verifies a signed challenge.

1. First visit to `/admin` shows **"Set up biometric sign-in"** — click **Enroll this device**, approve the OS prompt, and you're auto-signed-in.
2. Future visits show **"Sign in with biometrics"** — one click + biometric prompt.

If your machine does not have a platform authenticator (older laptops without Windows Hello, no Touch ID, etc.), a USB security key (e.g. YubiKey) will work. If none of those are available, the WebAuthn flow can be discussed during the walkthrough — the backend integration tests issue real JWTs end-to-end, so the auth wiring is exercised regardless of whether you log in live.

---

## 5. Tests

```powershell
# In the backend repo
dotnet test NPS.slnx
```

60 tests pass (38 unit + 22 integration).

---

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `ERR_CONNECTION_REFUSED` on `https://localhost:7287` | Backend was started on the `http` profile only. Restart with `--launch-profile https`. |
| Browser shows "Not secure" on `https://localhost:5173` | mkcert CA not in your trust store. Run `mkcert -install` (UAC prompt) and fully restart Chrome (`chrome://restart`). |
| `Login failed for user '<machine>\Guest'` from EF Core | The connection string in `appsettings.json` doesn't match `SA_PASSWORD` in `.env`. Align them. |
| WebAuthn prompt never appears | The page must be on HTTPS (or `http://localhost` exactly). Check the URL — Chrome hides the scheme by default; click into the URL bar to confirm. |
