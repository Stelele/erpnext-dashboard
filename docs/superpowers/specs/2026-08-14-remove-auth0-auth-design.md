# Replace Auth0 with Local Session Auth

> **Goal:** Remove Auth0 entirely. Replace it with a simple email + password login that keeps the user signed in forever (until manual logout), with email-based password setup and reset, and Admin/Viewer roles.

**Tech Stack:** .NET 10, MediatR, EF Core, ASP.NET Core `PasswordHasher<T>`, MailKit/SMTP, Vue 3, Pinia, Dexie/IndexedDB

---

## Problem Summary

Auth0 is counterproductive for the PWA. The SPA flow requires redirects, silent-login recovery, and refresh-token rotation that fail intermittently on mobile (recent commits: "added logout for failed silent login", "fixing login issues and bugs"). The only Auth0 feature actually relied upon is email-based password setup/reset.

The replacement is a self-hosted session auth:
- **Opaque session tokens** stored in PostgreSQL, no expiry — user stays signed in until they manually log out.
- **Email-based password setup and reset** via SMTP (MailKit).
- **Roles** (Admin/Viewer) mapped to the existing permission scope claims, so the current `RequireAuthorization` policies keep working.

---

## Architecture

### Backend — New Files

```
backend/Domain/Users/
├── Role.cs                        — enum { Admin, Viewer }

backend/Infrastructure/Email/
├── IEmailSender.cs                — interface: SendAsync(EmailMessage, ct)
├── SmtpEmailSender.cs             — MailKit implementation
├── EmailMessage.cs                — To, Subject, HtmlBody
├── SmtpOptions.cs                 — Host, Port, Username, Password, From

backend/Endpoints/Endpoints/
├── AuthEndpoints.cs               — /auth/login, /auth/logout, /auth/me,
│                                    /auth/forgot-password, /auth/reset-password

backend/Endpoints/Authentication/
├── SessionAuthenticationHandler.cs — validates opaque token → claims
├── SessionAuthenticationOptions.cs

backend/Application/Auth/
├── LoginCommand.cs / Handler.cs           — validate credentials, create session
├── LogoutCommand.cs / Handler.cs          — delete session row
├── GetCurrentUserQuery.cs / Handler.cs    — /auth/me profile + role
├── ForgotPasswordCommand.cs / Handler.cs  — mint reset token + email
├── ResetPasswordCommand.cs / Handler.cs   — set password, revoke sessions
├── CreatePasswordResetTokenCommand.cs     — shared token minting (used by create-user + forgot)
├── PasswordHasherProvider.cs              — wraps PasswordHasher<User> + shared salt/iterations
```

### Backend — Modified Files

```
backend/Domain/Users/User.cs                 — + PasswordHash, Role, FailedLoginCount, LockoutUntil; − Auth0UserId
backend/Infrastructure/Models/UserEntity.cs  — map new columns, drop Auth0UserId
backend/Infrastructure/Models/
├── SessionEntity.cs                          — new table config (TokenHash unique index)
├── PasswordResetTokenEntity.cs               — new table config
backend/Application/Users/CreateUserCommandHandler.cs — no Auth0; save role, email setup link
backend/Application/Users/CreateUserCommand.cs           — + role
backend/Application/Requests/CreateUserRequest.cs       — + role
backend/Application/Users/DeleteUserCommandHandler.cs   — no Auth0 delete
backend/Application/Users/UpdateUserRoleCommand.cs (new) / Handler.cs
backend/Endpoints/Endpoints/UsersEndpoints.cs          — + PATCH /users/{id}/role
backend/Endpoints/DependancyInjection.cs               — replace AddJwtBearer with session scheme
backend/Host/Middleware/UserContextMiddleware.cs       — read user_id claim directly
backend/Application/DependancyInjection.cs             — register auth commands, email, hasher
backend/Infrastructure/DependancyInjection.cs          — remove Auth0 provisioner registration
backend/Host/appsettings.json                          — Auth0:* → Email:Smtp:* + App:FrontendUrl
backend/docker-compose.yml                             — update env vars
```
Remove: `backend/Infrastructure/Auth0/Auth0UserProvisioner.cs`, `Auth0.AuthenticationApi` / `Auth0.ManagementApi` packages, `TestAuth0UserProvisioner.cs`.

### Frontend — New Files

```
frontend/src/views/LoginView.vue         — email + password form
frontend/src/views/ResetPasswordView.vue — reads ?token=, set new password; reset auto-logs in → redirect to /
frontend/src/guards/auth.ts              — route guard (token present?)
```

### Frontend — Modified Files

```
frontend/src/main.ts                     — remove createAuth0
frontend/src/routes/index.ts             — remove authGuard, add /login + /reset-password
frontend/src/stores/AuthStore.ts         — login/logout/update rewrite, drop refresh logic
frontend/src/services/api/index.ts       — attach token, 401 → redirect to /login
frontend/.env                            — remove VITE_AUTH0_*
frontend/package.json                    — remove @auth0/auth0-vue
```
Remove: `Auth0` config in `.github/workflows/deploy.yml`, `README`/`backend/README`/`frontend/README` references.

---

## Data Model

### User (modified)

| Column            | Type        | Notes                                  |
|-------------------|-------------|----------------------------------------|
| `PasswordHash`    | string?     | null until set via email link          |
| `Role`            | string      | `Admin` \| `Viewer`                    |
| `FailedLoginCount`| int         | reset on success                       |
| `LockoutUntil`    | datetime?   | null = not locked                      |
| ~~`Auth0UserId`~~ | —           | dropped in migration                   |

### Sessions (new)

| Column      | Type    | Notes                               |
|-------------|---------|-------------------------------------|
| `Id`        | Guid    | PK                                  |
| `UserId`    | Guid    | FK → User (cascade delete)          |
| `TokenHash` | string  | SHA-256 hex, unique index           |
| `CreatedOn` | datetime|                                     |
| `LastUsedOn`| datetime| updated on each authenticated call  |

No expiry column — a session lives until its row is deleted (manual logout, password reset, role change, user deletion).

### PasswordResetTokens (new)

| Column      | Type     | Notes                            |
|-------------|----------|----------------------------------|
| `Id`        | Guid     | PK                               |
| `UserId`    | Guid     | FK → User (cascade delete)       |
| `TokenHash` | string   | SHA-256 hex, unique index        |
| `CreatedOn` | datetime |                                  |
| `ExpiresOn` | datetime | 24h TTL                         |
| `UsedOn`    | datetime?| set on successful use            |

One table powers both initial password setup (on `POST /users`) and forgot-password.

One EF Core migration ships all three changes.

---

## Backend Auth Flow

### Session authentication handler

Replaces `AddJwtBearer`:

1. Read `Authorization: Bearer <token>`.
2. SHA-256 hash the token, look up `Sessions` by `TokenHash` (Include User).
3. Found + user exists → set claims and authenticate:
   - `user_id` = user.Id
   - `name`, `email`, `role`
   - `scope` = space-separated permissions derived from role
4. Update `LastUsedOn`.
5. Not found → no identity → 401.

### Permission mapping

- `Admin` → all 16 permissions (read/write users, companies, sites, expenses).
- `Viewer` → `read:users`, `read:companies`, `read:sites`, `read:expenses`.

`HasScopeHandler` and all `.RequireAuthorization(...)` calls remain unchanged.

### `UserContextMiddleware`

Change the namespace-claim JSON read to a direct `user_id` claim read. DB lookup of user + companies per request stays the same.

---

## Auth Endpoints

| Method | Route                    | Auth    | Behavior                                                                 |
|--------|--------------------------|---------|--------------------------------------------------------------------------|
| POST   | `/auth/login`            | public  | validate email+password (respect lockout); create session; return `{ token, user }` where user includes role |
| POST   | `/auth/logout`           | session | delete session row for presented token; idempotent 204                    |
| GET    | `/auth/me`               | session | return current user profile + role                                        |
| POST   | `/auth/forgot-password`  | public  | always 200; if email exists, mint token + email link                      |
| POST   | `/auth/reset-password`   | public  | validate token, set new password, mark token used, revoke all user sessions, return new `{ token, user }` |
| PATCH  | `/users/{id}/role`       | Admin   | update role, revoke that user's sessions                                  |

Lockout: after 5 consecutive failed logins for an email, `LockoutUntil` = now + 15 minutes. Successful login resets the counter.

---

## Email

- `IEmailSender` + `SmtpEmailSender` (MailKit) registered in Infrastructure DI.
- `SmtpOptions` (`Host`, `Port`, `Username`, `Password`, `From`, `EnableSsl`) from `Email:Smtp:*` config; stored in user-secrets / CI secrets.
- `App:FrontendUrl` config supplies the base URL for email links.
- Two templates:
  - **Password setup** — sent by `CreateUserCommandHandler` when a new user is created.
  - **Password reset** — sent by `ForgotPasswordCommandHandler`.
  - Both link to `{App:FrontendUrl}/reset-password?token=...`.
- Tests use a stub `IEmailSender` that records messages in memory.

---

## Frontend Auth Flow

### Routes

- `/login` → `LoginView.vue`; `/reset-password` → `ResetPasswordView.vue`.
- Custom guard: if `authStore` has no token → redirect to `/login`. If on `/login` or `/reset-password` and a token exists → redirect to `/`.

### `AuthStore`

- `login(email, password)` → `POST /auth/login`, persist `token` + `user` to localStorage.
- `update()` (app boot) → restore token from localStorage, `GET /auth/me` to hydrate profile, companies, sites. No JWT parsing, no namespace claim, no refresh timers.
- `triggerLogout()` → `POST /auth/logout`, clear localStorage + cache DB, redirect `/login`. Existing callers in `DashboardLayout.vue` and `MobileSettingsDrawer.vue` unchanged.
- Delete: `getAccessTokenSilently`, `refreshToken`, refresh timer, `visibilitychange` handler, `@@auth0` key cleanup.

### API client

Attach the stored token as `Authorization: Bearer`. On 401: clear session + redirect to `/login`. No silent-refresh retry — sessions are long-lived, so a 401 means the session was revoked.

### Cache sync worker

Unchanged; receives the token via `SET_CONFIG`.

---

## Removal Checklist

- `@auth0/auth0-vue` package (frontend `package.json` + lockfile).
- `createAuth0` in `main.ts`; `authGuard` in `routes/index.ts`.
- `VITE_AUTH0_*` from `frontend/.env`; `Auth0:*` config from `backend/Host/appsettings.json`, `backend/docker-compose.yml`, `.github/workflows/deploy.yml`.
- `Auth0.AuthenticationApi` + `Auth0.ManagementApi` NuGet packages.
- `backend/Infrastructure/Auth0/` (`Auth0UserProvisioner.cs`, `TemporaryPasswordGenerator`).
- `backend/Tests/TestAuth0UserProvisioner.cs`; Auth0 JWT claims in `TestAuthHandler`/`IntegrationTestFactory` replaced with real session tokens.
- Auth0 references in README files.

---

## Existing Users

`Auth0UserId` is dropped. Existing rows have no `PasswordHash`, so they cannot log in until they complete the reset flow. Release note: admins trigger forgot-password (or it's noted) so existing users set a password. No backfill of hashes is possible.

---

## Testing

### Backend (xUnit)

- **Login**: success returns token; wrong password fails; 5 failures → lockout; lockout resets on success/cooldown.
- **Session auth**: valid token authenticates with correct `scope` claims; garbage/missing token → 401; logout revokes (subsequent request → 401).
- **Role→scope**: Admin passes write-policy endpoint; Viewer 403 on write, passes read endpoints.
- **Reset flow**: forgot-password on existing email mints token + emails link (stub sender); reset with valid token sets password + revokes sessions; expired/reused token rejected.
- **CreateUser**: saves role, sends setup email via stub, no Auth0 dependency.
- **DeleteUser**: removes sessions + reset tokens via cascade.

Update `IntegrationTestFactory` to register stub `IEmailSender` and issue real session tokens. Remove `TestAuth0UserProvisioner` and the fake-Auth0 JWT helper.

### Frontend

No test framework exists today. Verification via `npm run build` + manual smoke checklist:

1. Login with correct/incorrect credentials.
2. Hard reload keeps you signed in (token in localStorage + `/auth/me` restore).
3. Manual logout revokes server-side (reopen → login page).
4. Forgot-password link arrives and works; resetting revokes other sessions.
5. Create a Viewer user → read-only; Admin → full.
6. Revoked session (role change/user delete) → next request lands on `/login`.

### Commands

```
dotnet build
dotnet test
npm run build
```

---

## Out of Scope

- Self-service "change password" while logged in (no UI exists for it; can reuse reset flow).
- User-management UI page (API-only, as today).
- Session expiry / idle timeout (deliberately "signed in forever").
- Password strength policy beyond a minimum length.
- Multi-device session management UI (sessions are per-device by design; reset/role-change revokes all).