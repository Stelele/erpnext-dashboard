# Company Chart Colors — Design Spec

**Date:** 2026-06-13
**Status:** Approved

## Overview

Generate chart color palettes algorithmically from the company's primary color, with per-primary lookup caching in IndexedDB. Chart colors (used by Bar, Line, Doughnut, Bubble charts and NumberCard trend indicators) harmonize with the company brand instead of being hardcoded.

## Chart Palette Strategy

Balanced hybrid: **4 analogous + 3 contrast + 1 neutral = 8 colors**

| Position | Type | Derivation |
|----------|------|-----------|
| 1-4 | Analogous | Primary hue ± small rotations, varied lightness for distinction |
| 5-7 | Contrast | Complementary hue zone (primary + 180°) + adjacent warm/cool |
| 8 | Neutral | Neutral-400 or 500 from the company's neutral palette |

Doughnut slice borders use neutral-900 (company neutral palette).

## Backend

### New Endpoint

`GET /api/theme/chart-colors?primaryColor={primaryColor}`

- **File:** `backend/Endpoints/Endpoints/ThemeEndpoints.cs` (new)
- **Auth:** `Permissions.ReadCompanies` (every authenticated user needs charts)
- **Response:**

```json
{
  "primaryColor": "emerald",
  "chartColors": ["#00C16A", "#10B981", "#0EA5E9", "#A855F7", "#0EA5E9", "#F97316", "#F43F5E", "#64748B"]
}
```

- **DTO:** `Application/DTOs/ChartColorsResponse.cs`
- **Handler:** `Application/ChartColors/GetChartColorsQuery.cs` (or inline query)
- **Validation:** `primaryColor` query param validated against `PrimaryColor` enum values
- **Algorithm:** All 18 palettes precomputed as constant data — since all possibilities are known, no runtime calculation needed. Each primary maps to its 8-color chart palette statically.

## Frontend

### IndexedDB Caching

Package: `idb-keyval` (lightweight IndexedDB key-value)

New composable: `frontend/src/composables/useChartColors.ts`

```ts
// Uses idb-keyval for simple get/set
const cache = idbKeyval

async function getChartColors(primaryColor: PrimaryColor): Promise<string[]> {
  const key = `chart-colors:${primaryColor}`
  const cached = await cache.get<string[]>(key)
  if (cached) return cached

  const api = await ApiSingleton.getInstance()
  const { data } = await api.GET("/api/theme/chart-colors", {
    params: { query: { primaryColor } }
  })
  if (data?.chartColors?.length) {
    await cache.set(key, data.chartColors)
    return data.chartColors
  }
  return [] // fallback
}
```

### Chart Component Updates

`ChartJsColors.ts` becomes a reactive store:

```ts
// Replaces hardcoded arrays with a ref
const chartColors = ref<string[]>([])
export function getChartJsColor(index: number) {
  if (chartColors.value.length > 0) {
    return chartColors.value[index % chartColors.value.length]
  }
  // Fallback to default palette
  return DEFAULT_COLORS[index % DEFAULT_COLORS.length]
}
export function setChartColors(colors: string[]) {
  chartColors.value = colors
}
```

- **CardBarChart, CardLineChart, CardDoughnutChart, CardBubbleChart:** import `setChartColors` and `getChartJsColor` — no template changes needed
- **CardDoughnutChart, CardBarChart:** border color `#0f172b` replaced with neutral-900 from company palette via CSS variable
- **NumberCard.vue:** trend colors derive from chart palette (up = green-500, down = primary-500, flat = neutral-500)

### Integration

Called in `useCompanyTheme.loadAndApply()` alongside primary/neutral palette application:

```ts
const chartColors = await getChartColors(settings.primaryColor)
setChartColors(chartColors)
```

### Fallback

If API fails or no settings exist, keep the existing 8-color hardcoded palette as default.

## Scope Boundaries

| In Scope | Out of Scope |
|---|---|
| 8 chart colors per primary (precomputed) | Per-neutral chart color variants |
| IndexedDB caching | Cache invalidation (colors never change per primary) |
| Chart.js dataset colors (fill, border, slices) | Axis/tick label colors (keep #fff/#000, unrelated to theme) |
| NumberCard trend colors | Real-time chart theme switching (already re-renders on data change) |
