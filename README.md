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

## ERPNext Integration

### Purchase Cycle API

Simplifies purchase entry for single-user businesses by creating the full purchase cycle (Purchase Order → Purchase Receipt → Purchase Invoice → Payment) in one API call.

**Endpoint:** `POST /api/method/runservermethod?script_name=create_full_purchase`

**Authentication:** Requires valid Frappe session or API key/token

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| company | string | Yes | Company name |
| supplier | string | Yes | Supplier name |
| warehouse | string | Yes | Receiving warehouse |
| items | array | Yes | Array of `{item_code, qty, rate}` objects |
| invoice_number | string | Yes | Supplier's invoice number |
| invoice_date | string | Yes | Supplier's invoice date (YYYY-MM-DD) |

**Example Request:**
```json
{
  "company": "My Company",
  "supplier": "ABC Suppliers",
  "warehouse": "Stores - MC",
  "items": [
    {"item_code": "ITEM-001", "qty": 10, "rate": 100},
    {"item_code": "ITEM-002", "qty": 5, "rate": 200}
  ],
  "invoice_number": "SUP-INV-2024-001",
  "invoice_date": "2024-01-15"
}
```

**Success Response (200):**
```json
{
  "data": {
    "purchase_order": "PO-00001",
    "purchase_receipt": "PR-00001",
    "purchase_invoice": "PI-00001",
    "payment_entry": "PE-00001"
  }
}
```

**Error Response (400/500):**
```json
{
  "exc": "[\"frappe.exceptions.ValidationError: Supplier 'XYZ' does not exist\"]"
}
```

**Notes:**
- Payment is always made via Cash mode of payment
- Requires "Cash" Mode of Payment to exist in ERPNext
- Full transaction rollback: if any step fails, all changes are reverted
- Duplicate invoice numbers are rejected

## License

MIT