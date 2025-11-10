# Njeremoto Enterprise Dashboard

A Vue 3 + TypeScript dashboard that surfaces point‑of‑sale insights captured in ERPNext, presenting sales and operational metrics in a clearer, more actionable way.

## Features
- Overview dashboard with key KPIs and trends.
- Bar and doughnut charts (Chart.js via `vue-chartjs`).
- Clean UI components using `@nuxt/ui`.
- Vue Router for modular, multi‑page navigation.
- Designed to integrate with ERPNext’s REST API.

## Tech Stack
- Vue 3, TypeScript, Vite
- Chart.js + `vue-chartjs`
- `@nuxt/ui` (component library)
- Vue Router

## Quick Start
- Prerequisites: Node.js 18+ and npm 9+
- Install: `npm install`
- Dev: `npm run dev` (serves with HMR)
- Build: `npm run build` (type‑check + production build)
- Preview: `npm run preview` (serves the built app)

## Project Structure
- `src/App.vue`: App shell with header, sidebar, and router view
- `src/main.ts`: App bootstrap, UI/plugin setup
- `src/routes/`: Router setup and routes
- `src/views/OverviewView.vue`: Main dashboard view with sample charts and KPI cards
- `src/components/`: Reusable UI blocks (`NumberCard`, `CardBarChart`, `CardDoughnutChart`, `SideBar`)
- `src/utils/NavItems.ts`: Navigation items for sidebar
- `src/style.css`: Tailwind and `@nuxt/ui` base imports
- `vite.config.ts`: Vite and `@nuxt/ui` plugin configuration

## ERPNext Integration
- Current status: The UI uses sample data. Next step is wiring to ERPNext’s REST API.
- Recommended approach:
  - Configure environment variables (create a local `.env`):
    - `VITE_ERP_BASE_URL=https://your-erpnext.example.com`
    - `VITE_ERP_API_KEY=...`
    - `VITE_ERP_API_SECRET=...`
  - Use ERPNext token auth: set `Authorization: token API_KEY:API_SECRET` on requests.
  - For local dev, use a Vite proxy (if needed) to avoid CORS, or enable CORS on ERPNext.
  - Add a lightweight API client (e.g., `src/utils/erp.ts`) to centralize calls like sales summaries, top items, and category breakdowns.
- Initial endpoints to target:
  - `/api/resource/Sales Invoice` (filters by date range, POS Profile)
  - `/api/resource/Sales Invoice Item` (aggregation for top products)
  - `/api/method/frappe.desk.reportview.get` (for custom queries where helpful)

## Development Tips
- Add new dashboard sections as routed views under `src/views` and register in `src/routes`.
- Reuse `NumberCard`, `CardBarChart`, and `CardDoughnutChart` for consistent visuals.
- Keep data fetching in composables or `src/utils/erp.ts` to separate concerns.

## Troubleshooting
- Blank UI or missing styles: verify `src/style.css` includes `@nuxt/ui` and Tailwind imports.
- Charts not rendering: ensure datasets and labels arrays are aligned and not empty.
- CORS during API calls: configure ERPNext CORS or add a Vite dev proxy.

## Roadmap
- Wire real ERPNext data into Overview.
- Add filters (date range, location, cashier) with persisted state.
- Add Sales, Products, and Reports views with drill‑downs.
- Export to CSV/PDF for common reports.

## Scripts
- `npm run dev`: Start dev server
- `npm run build`: Type‑check and build for production
- `npm run preview`: Preview the production build
