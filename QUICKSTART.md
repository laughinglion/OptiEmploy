# EmploymentVerify — Quick Start Guide

Employment verification SaaS for South African companies.
Built with .NET 10, Blazor Server, CQRS, PostgreSQL.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- An SMTP account (Gmail, SendGrid, or your own mail server)

---

## 1. Configure the API (`src/EmploymentVerify.Api/appsettings.json`)

### Database
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=employment_verify;Username=postgres;Password=YOUR_PASSWORD"
}
```

### JWT Secret (must be at least 32 characters — use a random string)
```json
"Jwt": {
  "SecretKey": "replace-with-a-long-random-secret-key-at-least-64-chars"
}
```

To generate a secure key:
```bash
# PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

# or openssl
openssl rand -base64 48
```

### SMTP Email

**Gmail (recommended for testing):**
1. Enable 2FA on your Gmail account
2. Go to Google Account → Security → App Passwords → generate one
3. Use that 16-char app password (not your regular password)

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "you@gmail.com",
  "Password": "your-16-char-app-password",
  "FromAddress": "noreply@yourcompany.co.za",
  "FromName": "Employment Verify",
  "EnableSsl": true
}
```

**SendGrid:**
```json
"Smtp": {
  "Host": "smtp.sendgrid.net",
  "Port": 587,
  "Username": "apikey",
  "Password": "your-sendgrid-api-key",
  "FromAddress": "noreply@yourcompany.co.za",
  "FromName": "Employment Verify",
  "EnableSsl": true
}
```

---

## 2. Configure the Web App (`src/EmploymentVerify.Web/appsettings.json`)

```json
"ApiBaseUrl": "https://localhost:5001"
```

This must match the URL the API runs on. In production, set it to your actual API domain.

### Optional: Google SSO

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a project → Credentials → OAuth 2.0 Client ID → Web Application
3. Add Authorized redirect URI: `https://localhost:5002/signin-google`
4. Copy Client ID and Client Secret:

```json
"Authentication": {
  "Google": {
    "ClientId": "123456789-xxx.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-xxxxxxxxxxxx"
  }
}
```

### Optional: Microsoft SSO

1. Go to [Azure Portal](https://portal.azure.com/) → Entra ID → App registrations → New
2. Add redirect URI: `https://localhost:5002/signin-microsoft`
3. Create a client secret under Certificates & secrets

```json
"Authentication": {
  "Microsoft": {
    "ClientId": "your-app-client-id-guid",
    "ClientSecret": "your-client-secret-value",
    "TenantId": "common"
  }
}
```

Leave `ClientId` and `ClientSecret` empty to disable SSO (email/password login still works).

---

## 3. Set Up the Database

```bash
# From the repo root
cd C:/code/employment

# Apply all migrations
dotnet ef database update --project src/EmploymentVerify.Infrastructure --startup-project src/EmploymentVerify.Api
```

The API also auto-migrates on startup in Development mode.

---

## 4. Run the Applications

Open two terminals:

**Terminal 1 — API:**
```bash
cd C:/code/employment
dotnet run --project src/EmploymentVerify.Api
# Runs on https://localhost:5001
```

**Terminal 2 — Web:**
```bash
cd C:/code/employment
dotnet run --project src/EmploymentVerify.Web
# Runs on https://localhost:5002 (or http://localhost:5000)
```

Then open: `https://localhost:5002`

---

## 5. First-Time Setup

### Create the first Admin user

After running, register a user via the web UI at `/account/register`.

Then in psql or your DB tool, promote them to Admin:
```sql
UPDATE "users"
SET role = 'Admin'
WHERE email = 'your@email.com';
```

### Add companies to the directory

Log in as Admin → navigate to **Admin → Companies** → add the companies you'll verify against.

For each company you add:
- Fill in the HR contact name, email, and phone
- Toggle **Force Call** ON if you always want phone verification for that company (skips email)

### Add credits to a requestor account

Log in as Admin → **Admin → User Management** → enter the user's ID and credit amount.

Each verification costs 1 credit by default (adjust in code if needed).

---

## 6. How the Verification Flow Works

```
Requestor submits verification
         │
         ▼
Is company in directory?
    │           │
   YES          NO
    │           │
Is ForceCall?   └──► Route to Operator Work Queue
    │
   YES/NO
    │
   NO ──► Send email to HR with unique link (48h expiry)
              │
              ▼
         HR clicks link → /verify/confirm?token=xxx
         HR confirms/corrects/denies
              │
              ▼
         Status → Confirmed / Denied
         Requestor sees result in My Requests
    │
   YES ──► Route to Operator Work Queue
              │
              ▼
         Operator calls HR → records outcome
         Status → Confirmed / Denied / Unreachable
```

---

## 7. Project Structure

```
src/
  EmploymentVerify.Domain/          # Entities, enums, no dependencies
  EmploymentVerify.Application/     # CQRS commands & queries (MediatR)
  EmploymentVerify.Infrastructure/  # EF Core, SMTP, JWT, BCrypt
  EmploymentVerify.Api/             # Minimal API endpoints
  EmploymentVerify.Web/             # Blazor Server frontend
tests/
  EmploymentVerify.Tests/           # Unit & integration tests
```

---

## 8. User Roles

| Role | Can Do |
|------|--------|
| **Requestor** | Submit verifications, view own history, manage credits |
| **Operator** | View & action work queue (phone calls), view all verifications |
| **Admin** | Everything above + manage companies, users, view stats |

---

## 9. POPIA Compliance Notes

- Every verification submission requires explicit consent affirmation
- Consent type (requestor-warranted vs. direct employee) is stored per request
- Request logging middleware records all data access for audit purposes
- SA ID numbers are stored in the database — ensure PostgreSQL is on encrypted storage in production
- Do not store verification reports longer than necessary — implement a data retention policy

---

## 10. Production Deployment Checklist

- [ ] Replace all `appsettings.json` values with environment variables or a secrets manager
- [ ] Generate a strong JWT secret (64+ chars)
- [ ] Use a dedicated PostgreSQL user with least-privilege access
- [ ] Enable HTTPS with a valid TLS certificate
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure PostgreSQL with encrypted storage
- [ ] Set up automated database backups
- [ ] Review and tune the SMTP rate limits
- [ ] Set up monitoring/alerting (health endpoint: `/health` if configured)
