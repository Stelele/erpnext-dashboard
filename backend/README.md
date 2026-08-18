# Backend API

.NET 10 ASP.NET Core REST API for the Njeremoto Dashboard.

## Architecture

Clean Architecture with four main layers:

```
Api/
├── Domain/           # Entities, business rules (no external dependencies)
├── Application/     # Use cases, MediatR handlers, commands & queries
├── Infrastructure/ # Database context, EF Core, external services
├── Endpoints/       # Minimal API route definitions
└── Host/            # Entry point, configuration, DI setup
```

## Tech Stack

- **Runtime**: .NET 10
- **Framework**: ASP.NET Core
- **ORM**: Entity Framework Core 9
- **Database**: PostgreSQL (via Npgsql)
- **CQRS**: MediatR
- **API Docs**: Scalar (OpenAPI)
- **Auth**: Session-based (email + password) with Bearer session token

## Configuration

### appsettings.json

Key settings in `Host/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=dashboard;Username=postgres;Password=postgres"
  },
  "App": {
    "FrontendUrl": "https://dashboard.njeremoto.net"
  },
  "Email": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "your-smtp-user",
      "Password": "your-smtp-password",
      "From": "dashboard@example.com"
    }
  }
}
```

Auth is session-based: login via `POST /auth/login`, the API returns a Bearer session token, and password reset flows through `POST /auth/forgot-password` / `POST /auth/reset-password`. Users have `Admin` or `Viewer` roles.

### User Secrets

For local development, store sensitive values in user secrets:

```bash
cd Host
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=dashboard;Username=postgres;Password=postgres"
dotnet user-secrets set "Email:Smtp:Host" "smtp.example.com"
dotnet user-secrets set "Email:Smtp:Password" "your-smtp-password"
dotnet user-secrets set "MediatR:LicenseKey" "your-mediatr-key"
```

## Running Locally

### Option 1: CLI

```bash
# Restore and run
cd Api
dotnet restore
dotnet run --project Host/Host.csproj
```

The API will be available at `http://localhost:8000`.

### Option 2: Docker

```bash
cd ../Env
docker compose up -d postgres

# Build and run the API container
cd ../Api
docker build -t dashboard-api .
docker run -p 8000:8000 --env-file .env.local dashboard-api
```

## API Documentation

When running in development, access the interactive API docs at:

```
http://localhost:8000/scalar/v1
```

## Database

The application uses EF Core migrations. After setting up PostgreSQL:

```bash
cd Host
dotnet ef database update
```

Or let EF Core create the database on startup (configured in `Program.cs`).

## Project References

- `Host` → Application, Domain, Infrastructure, Endpoints
- `Application` → Domain
- `Infrastructure` → Domain
- `Endpoints` → Application, Domain
