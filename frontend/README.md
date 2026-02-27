# Njeremoto Enterprise Dashboard

A Vue 3 + TypeScript dashboard that surfaces point-of-sale insights captured in ERPNext, presenting sales, expenses, and stock metrics in a clearer, more actionable way.

## Features

- **Overview Dashboard** - Key KPIs with bar/doughnut charts showing sales trends and breakdowns
- **Expenses Tracking** - View and add expenses via journal entries
- **Sales Analytics** - Detailed sales data with period-based filtering
- **Stock Levels** - Current inventory status from warehouse
- **Period Filtering** - Filter data by Today, Yesterday, This Week, Last Week, This Month, Last Month, This Quarter, Last Quarter, This Semester, Last Semester, This Year, Last Year
- **Authentication** - Auth0-based authentication protecting all routes
- **PWA Support** - Service worker enabled for offline capability

## Tech Stack

- Vue 3, TypeScript, Vite
- Chart.js + vue-chartjs for visualizations
- @nuxt/ui component library
- Vue Router for navigation
- Pinia for state management
- Auth0 for authentication
- ERPNext REST API integration
- Zod for schema validation
- openapi-fetch for type-safe API calls
- moment.js for date handling
- vite-plugin-pwa for PWA support

## Quick Start

- Prerequisites: Node.js 18+ and npm 9+
- Install: `npm install`
- Dev: `npm run dev` (serves with HMR)
- Build: `npm run build` (type-check + production build)
- Preview: `npm run preview` (serves the built app)

## Project Structure

```
src/
├── main.ts                 # App bootstrap, plugin setup
├── App.vue                 # Root component
├── routes/index.ts         # Router with Auth0 guards
├── views/
│   ├── OverviewView.vue    # Dashboard with KPIs and charts
│   ├── ExpensesView.vue    # Expenses listing and form
│   ├── SalesView.vue       # Sales data table
│   └── StockView.vue       # Stock levels table
├── components/
│   ├── NumberCard.vue      # KPI display card
│   ├── CardBarChart.vue    # Bar chart wrapper
│   ├── CardDoughnutChart.vue # Doughnut chart wrapper
│   ├── CardBubbleChart.vue # Bubble chart wrapper
│   ├── SideBar.vue         # Navigation sidebar
│   ├── ExpenseTable.vue    # Expenses data table
│   ├── SalesTable.vue      # Sales data table
│   ├── StockTable.vue      # Stock data table
│   ├── ExpenseForm.vue     # Add expense form
│   ├── LoadingBar.vue      # Loading indicator
│   └── CartTitle.vue       # Page title component
├── stores/
│   ├── AuthStore.ts        # Auth state and ERPNext credentials
│   ├── NavStore.ts         # Navigation state
│   ├── DataStore.ts        # General data store
│   ├── OverViewDataStore.ts # Overview page data
│   ├── SalesDataStore.ts   # Sales data management
│   ├── StockDataStore.ts   # Stock data management
│   ├── ExpenseDataStore.ts # Expenses data management
│   └── useStockDataStore.ts # Stock store (alternate)
├── services/
│   ├── ErpNextService.ts   # ERPNext API client
│   └── api/                # openapi-fetch generated API
├── types/                  # TypeScript type definitions
├── utils/                  # Helper functions
└── layouts/                # Layout components
```

## Environment Variables

Create a `.env.local` file with:

```env
VITE_AUTH0_DOMAIN=your-tenant.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://your-api-identifier
VITE_ERP_URL=https://your-erpnext-instance.com
VITE_ERP_TOKEN=your-erpnext-api-token
VITE_ERP_COMPANY=Your Company Name
```

## ERPNext Integration

The dashboard integrates directly with ERPNext's REST API:

- **Sales Data** - `/api/v2/method/get_sales`, `/api/v2/method/grouped_sales_summary`
- **Expenses** - `/api/v2/method/grouped_expenses_summary`, `/api/v2/method/get_journal_entries`
- **Purchases** - `/api/v2/method/grouped_purchase_invoice_summary`
- **Stock** - `/api/v2/method/get_stock_levels`, `/api/v2/method/get_average_stock_value`
- **Accounts** - `/api/v2/method/account_names`

All requests use token-based authentication with the ERPNext API.

## Authentication

- Auth0 handles user authentication
- All routes are protected with `authGuard`
- ERPNext credentials stored in AuthStore after login

## Scripts

- `npm run dev` - Start dev server
- `npm run dev:host` - Start dev server with host binding
- `npm run build` - Type-check and build for production
- `npm run preview` - Preview the production build
