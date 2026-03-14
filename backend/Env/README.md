# Infrastructure

Docker Compose configuration for local development dependencies.

## Services

### PostgreSQL

- **Image**: `postgres:latest`
- **Port**: `5432:5432`
- **Database**: `dashboard`
- **Credentials**: 
  - Username: `postgres`
  - Password: `postgres`

## Quick Start

```bash
cd backend/Env
docker compose up -d
```

This starts a PostgreSQL container accessible at `localhost:5432`.

## Connection String

For local development, use:

```
Host=localhost;Database=dashboard;Username=postgres;Password=postgres
```

## Managing the Database

### Connect via CLI

```bash
docker exec -it dashboard-env-postgres-1 psql -U postgres -d dashboard
```

### Run Migrations

From the `Api/Host` directory:

```bash
dotnet ef database update
```

### Reset the Database

```bash
docker compose down -v
docker compose up -d
```

This removes the volume and recreates a fresh database.

## Environment Variables

The following variables are configured in `compose.yml`:

| Variable | Default |
|----------|---------|
| `POSTGRES_DB` | dashboard |
| `POSTGRES_USER` | postgres |
| `POSTGRES_PASSWORD` | postgres |

Override these in a `.env` file or directly in `compose.yml` for production.
