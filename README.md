# Njeremoto Dashboard

A full-stack business intelligence dashboard for monitoring ERP metrics including sales, stock, expenses, and site performance. Features offline support, interactive charts, and Excel data export.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Vue 3 + Vite + Nuxt UI + Pinia |
| Backend | .NET 10 ASP.NET Core |
| Database | PostgreSQL |
| Auth | Auth0 |
| Infrastructure | Docker |

## Project Structure

```
njeremoto-dashboard/
├── frontend/          # Vue 3 SPA with PWA support
├── backend/
│   ├── Api/           # .NET 10 API
│   └── Env/           # Docker Compose configuration
├── erpnext/           # ERPNext integration scripts
│   ├── optimize/      # Database optimization scripts
│   └── server_scripts/ # Server-side automation
├── designs/           # Database schema & wireframes
└── .github/
    └── workflows/      # CI/CD pipelines
```

## Quick Start

### Prerequisites

- Node.js 22+
- .NET 10 SDK
- Docker & Docker Compose

### Running Locally

1. **Start the database:**
   ```bash
   cd backend/Env
   docker compose up -d
   ```

2. **Configure the backend:**
   - Add connection string to user secrets:
     ```bash
     cd backend/Api/Host
     dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=dashboard;Username=postgres;Password=postgres"
     ```
   - Add Auth0 client secret and MediatR license key

3. **Run the backend:**
   ```bash
   cd backend/Api
   dotnet run --project Host/Host.csproj
   ```

4. **Configure the frontend:**
   Copy `.env` to `.env.local` and fill in your Auth0 credentials:
   ```
   VITE_AUTH0_DOMAIN=your-tenant.auth0.com
   VITE_AUTH0_CLIENT_ID=your-client-id
   VITE_AUTH0_AUDIENCE=your-api-audience
   VITE_API_URL=http://localhost:8000
   ```

5. **Run the frontend:**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

## Features

- **Dashboard Overview** - Key metrics and KPIs at a glance
- **Sales Tracking** - Monitor sales performance with charts
- **Stock Management** - Real-time inventory analytics
- **Expense Tracking** - Categorized expense reporting
- **Offline Support** - PWA with local data caching via SQL.js
- **Excel Export** - Export any data table to `.xlsx`
- **ERPNext Integration** - Database optimization and server-side automation scripts

## CI/CD Pipeline

The project uses GitHub Actions for automated builds and deployment:

- **Frontend**: Builds on Node.js 22, deploys to a separate GitHub Pages repo
- **Backend**: Docker image builds and pushes to Docker Hub on changes to `backend/Api/**`

Triggers:
- Push to `master` branch
- Manual workflow dispatch

## License

MIT