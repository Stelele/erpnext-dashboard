# Chart Brand Colors Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate chart color palettes per company primary color, precomputed in the backend and cached in IndexedDB on the frontend.

**Architecture:** Backend stores 18 precomputed palettes as static data behind a `GET /api/theme/chart-colors` endpoint. Frontend fetches lazily, caches in IndexedDB via `idb-keyval`, and makes ChartJsColors reactive. Chart components automatically pick up the new palette. Fallback keeps existing hardcoded colors.

**Tech Stack:** C# minimal API, `idb-keyval` (IndexedDB key-value), Chart.js via `vue-chartjs`, reactive refs.

---

## File Map

| File | Action | Purpose |
|------|--------|---------|
| `backend/Application/ChartColors/ChartColorData.cs` | Create | 18 precomputed palettes (PrimaryColor → 8 hex colors) |
| `backend/Application/DTOs/ChartColorsResponse.cs` | Create | Response DTO |
| `backend/Endpoints/Endpoints/ThemeEndpoints.cs` | Create | `GET /api/theme/chart-colors` endpoint |
| `backend/Endpoints/DependancyInjection.cs` | Modify | Register ThemeEndpoints |
| `frontend/package.json` | Modify | Add `idb-keyval` dependency |
| `frontend/src/utils/ChartJsColors.ts` | Modify | Make reactive, export `setChartColors` |
| `frontend/src/composables/useChartColors.ts` | Create | IndexedDB cache + API fetch |
| `frontend/src/composables/useCompanyTheme.ts` | Modify | Call chart color loading after palette |
| `frontend/src/components/NumberCard.vue` | Modify | Derive trend colors from chart palette |
| `frontend/src/components/CardBarChart.vue` | Modify | Border from neutral CSS var |
| `frontend/src/components/CardDoughnutChart.vue` | Modify | Border from neutral CSS var |

---

### Task 1: Backend — Chart Color Data

**Files:**
- Create: `backend/Application/ChartColors/ChartColorData.cs`

- [ ] **Step 1: Create the precomputed palette data**

```csharp
using Domain.CompanySettings;

namespace Application.ChartColors;

public static class ChartColorData
{
    private static readonly Dictionary<PrimaryColor, string[]> Palettes = new()
    {
        [PrimaryColor.Black] = ["#1a1a2e", "#16213e", "#0f3460", "#533483", "#e94560", "#f5a623", "#7ec8e3", "#a0aec0"],
        [PrimaryColor.Red] = ["#ef4444", "#dc2626", "#f97316", "#eab308", "#0ea5e9", "#10b981", "#8b5cf6", "#94a3b8"],
        [PrimaryColor.Orange] = ["#f97316", "#ea580c", "#eab308", "#ef4444", "#0ea5e9", "#8b5cf6", "#10b981", "#94a3b8"],
        [PrimaryColor.Amber] = ["#f59e0b", "#d97706", "#eab308", "#f97316", "#3b82f6", "#8b5cf6", "#ef4444", "#94a3b8"],
        [PrimaryColor.Yellow] = ["#eab308", "#ca8a04", "#f97316", "#f59e0b", "#3b82f6", "#8b5cf6", "#ef4444", "#94a3b8"],
        [PrimaryColor.Lime] = ["#84cc16", "#65a30d", "#22c55e", "#eab308", "#8b5cf6", "#ef4444", "#3b82f6", "#94a3b8"],
        [PrimaryColor.Green] = ["#22c55e", "#16a34a", "#10b981", "#84cc16", "#8b5cf6", "#ef4444", "#f97316", "#94a3b8"],
        [PrimaryColor.Emerald] = ["#10b981", "#059669", "#14b8a6", "#22c55e", "#8b5cf6", "#ef4444", "#f59e0b", "#94a3b8"],
        [PrimaryColor.Teal] = ["#14b8a6", "#0d9488", "#06b6d4", "#10b981", "#ef4444", "#f59e0b", "#8b5cf6", "#94a3b8"],
        [PrimaryColor.Cyan] = ["#06b6d4", "#0891b2", "#0ea5e9", "#14b8a6", "#ef4444", "#f97316", "#8b5cf6", "#94a3b8"],
        [PrimaryColor.Sky] = ["#0ea5e9", "#0284c7", "#3b82f6", "#06b6d4", "#f97316", "#ef4444", "#eab308", "#94a3b8"],
        [PrimaryColor.Blue] = ["#3b82f6", "#2563eb", "#6366f1", "#0ea5e9", "#f97316", "#ef4444", "#eab308", "#94a3b8"],
        [PrimaryColor.Indigo] = ["#6366f1", "#4f46e5", "#8b5cf6", "#3b82f6", "#f59e0b", "#ef4444", "#10b981", "#94a3b8"],
        [PrimaryColor.Violet] = ["#8b5cf6", "#7c3aed", "#a855f7", "#6366f1", "#f59e0b", "#10b981", "#ef4444", "#94a3b8"],
        [PrimaryColor.Purple] = ["#a855f7", "#9333ea", "#c084fc", "#8b5cf6", "#f59e0b", "#10b981", "#0ea5e9", "#94a3b8"],
        [PrimaryColor.Fuchsia] = ["#d946ef", "#c026d3", "#e879f9", "#a855f7", "#10b981", "#f59e0b", "#0ea5e9", "#94a3b8"],
        [PrimaryColor.Pink] = ["#ec4899", "#db2777", "#f472b6", "#d946ef", "#10b981", "#0ea5e9", "#f59e0b", "#94a3b8"],
        [PrimaryColor.Rose] = ["#f43f5e", "#e11d48", "#fb7185", "#ec4899", "#10b981", "#0ea5e9", "#f59e0b", "#94a3b8"],
    };

    public static string[]? GetColors(PrimaryColor primaryColor) =>
        Palettes.TryGetValue(primaryColor, out var colors) ? colors : null;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 2: Backend — Chart Colors Response DTO

**Files:**
- Create: `backend/Application/DTOs/ChartColorsResponse.cs`

- [ ] **Step 1: Create the response DTO**

```csharp
using Domain.CompanySettings;

namespace Application.DTOs;

public record ChartColorsResponse(
    PrimaryColor PrimaryColor,
    string[] ChartColors
);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 3: Backend — Theme Endpoint

**Files:**
- Create: `backend/Endpoints/Endpoints/ThemeEndpoints.cs`

- [ ] **Step 1: Create the endpoint**

```csharp
using Application.ChartColors;
using Application.DTOs;
using Domain.CompanySettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class ThemeEndpoints
{
    public static WebApplication MapThemeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/theme/chart-colors", (string? primaryColor) =>
            {
                if (primaryColor == null || !Enum.TryParse<PrimaryColor>(primaryColor, true, out var parsed))
                {
                    return Results.BadRequest($"Invalid primary color: {primaryColor}");
                }

                var colors = ChartColorData.GetColors(parsed);
                if (colors == null)
                {
                    return Results.BadRequest($"No palette for: {primaryColor}");
                }

                return Results.Ok(new ChartColorsResponse(parsed, colors));
            })
            .WithTags(Tags.Companies)
            .WithName("GetChartColors")
            .Produces<ChartColorsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequireAuthorization(Permissions.ReadCompanies);

        return app;
    }
}
```

- [ ] **Step 2: Register in DependancyInjection.cs**

Modify `backend/Endpoints/DependancyInjection.cs` — in the `MapApi` method, add the call between the existing endpoint registrations:

```csharp
app
    .MapCompanyEndpoints()
    .MapSitesEndpoints()
    .MapUsersEndpoints()
    .MapExpenseEndpoints()
    .MapThemeEndpoints();
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 4: Backend — Run Tests

- [ ] **Step 1: Build and run tests**

Run: `dotnet test Tests/ --no-build`
Expected: All 28 tests pass.

---

### Task 5: Frontend — Install idb-keyval

**Files:**
- Modify: `frontend/package.json` (via npm install)

- [ ] **Step 1: Install the dependency**

Run: `npm install idb-keyval`
Expected: Package added to `package.json` and `node_modules`.

---

### Task 6: Frontend — Make ChartJsColors Reactive

**Files:**
- Modify: `frontend/src/utils/ChartJsColors.ts`

- [ ] **Step 1: Replace hardcoded arrays with reactive ref and export setter**

Replace the file content:

```ts
import { ref } from 'vue'

const DEFAULT_COLORS = [
    '#00DC82',
    '#2563EB',
    '#F43F5E',
    '#A855F7',
    '#EAB308',
    '#0EA5E9',
    '#14B8A6',
    '#64748B'
]

const chartJsColors = ref<string[]>([])

export function getChartJsColor(index: number): string {
    const palette = chartJsColors.value.length > 0 ? chartJsColors.value : DEFAULT_COLORS
    return palette[index % palette.length]!
}

export function setChartColors(colors: string[]): void {
    chartJsColors.value = colors
}
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: Build succeeds.

---

### Task 7: Frontend — Chart Colors Composable

**Files:**
- Create: `frontend/src/composables/useChartColors.ts`

- [ ] **Step 1: Create the composable**

```ts
import { setChartColors } from '@/utils/ChartJsColors'
import { ApiSingleton } from '@/services/api'
import type { PrimaryColor } from '@/services/api/schema'

let idbKeyval: typeof import('idb-keyval') | null = null
async function getKeyval() {
  if (!idbKeyval) {
    idbKeyval = await import('idb-keyval')
  }
  return idbKeyval
}

async function getCached(key: string): Promise<string[] | undefined> {
  try {
    const kv = await getKeyval()
    return await kv.get(key)
  } catch {
    return undefined
  }
}

async function setCached(key: string, value: string[]): Promise<void> {
  try {
    const kv = await getKeyval()
    await kv.set(key, value)
  } catch {
    // Silently fail — cache is optional
  }
}

export function useChartColors() {
  async function loadChartColors(primaryColor: PrimaryColor | null | undefined): Promise<void> {
    if (!primaryColor) {
      setChartColors([])
      return
    }

    const cacheKey = `chart-colors:${primaryColor}`

    const cached = await getCached(cacheKey)
    if (cached && cached.length > 0) {
      setChartColors(cached)
      return
    }

    try {
      const api = await ApiSingleton.getInstance()
      const { data, error } = await api.GET('/api/theme/chart-colors', {
        params: { query: { primaryColor } },
      })
      if (data?.chartColors?.length) {
        setChartColors(data.chartColors as string[])
        await setCached(cacheKey, data.chartColors as string[])
      }
    } catch {
      // Fallback: keep defaults set by setChartColors([])
    }
  }

  return { loadChartColors }
}
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: Build succeeds.

---

### Task 8: Frontend — Integrate into useCompanyTheme

**Files:**
- Modify: `frontend/src/composables/useCompanyTheme.ts`

- [ ] **Step 1: Call chart color loading after palette application**

Add the import at the top:

```ts
import { useChartColors } from './useChartColors'
```

In `loadAndApply`, after `applyNeutralPalette(...)`, add:

```ts
const { loadChartColors } = useChartColors()
await loadChartColors(settings?.primaryColor ?? undefined)
```

The full updated `loadAndApply`:

```ts
export function useCompanyTheme() {
  const { loadChartColors } = useChartColors()

  async function loadAndApply(companyId: string): Promise<void> {
    const { useDataStore } = await import('@/stores/DataStore')
    const dataStore = useDataStore()
    const settings = await dataStore.getCompanySettings(companyId)

    applyPrimaryPalette(settings?.primaryColor ?? undefined)
    applyNeutralPalette(settings?.neutralColor ?? undefined)
    await loadChartColors(settings?.primaryColor ?? undefined)
  }

  return { loadAndApply }
}
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: Build succeeds.

---

### Task 9: Frontend — Update NumberCard Trend Colors

**Files:**
- Modify: `frontend/src/components/NumberCard.vue`

- [ ] **Step 1: Replace hardcoded trend colors with chart palette**

Remove the `colorMode` import and the hardcoded `trendColor` computation. Replace the script with:

```ts
<script setup lang="ts">
import { computed } from "vue";
import { getChartJsColor } from "@/utils/ChartJsColors";

export type ChangeDirection = "up" | "down" | "flat";
export interface NumberCardProps {
    title: string;
    value: number;
    direction?: ChangeDirection;
    percentChange?: number;
}
const props = defineProps<NumberCardProps>();

const formatedValue = computed(() => {
    if (Number.isNaN(props.value)) {
        return "-";
    }
    if (Math.abs(props.value) - Math.floor(Math.abs(props.value)) === 0) {
        return props.value;
    }
    return props.value.toFixed(2);
});

const icon = computed(() => {
    if (props.direction === "up") return "i-lucide-arrow-up-right";
    if (props.direction === "down") return "i-lucide-arrow-down-right";
    return "i-lucide-arrow-right";
});

const trendColor = computed(() => {
    if (props.direction === "up") return getChartJsColor(1);  // green slot
    if (props.direction === "down") return getChartJsColor(0); // primary slot
    return getChartJsColor(7); // neutral slot
});
</script>
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: Build succeeds.

---

### Task 10: Frontend — Border Colors from Neutral CSS Var

**Files:**
- Modify: `frontend/src/components/CardBarChart.vue`
- Modify: `frontend/src/components/CardDoughnutChart.vue`

- [ ] **Step 1: Replace hardcoded `#0f172b` with neutral-900 CSS variable**

In **CardBarChart.vue** line 90, replace:

```
borderColor: colorMode.value === "dark" ? "#0f172b" : undefined,
```

With:

```
borderColor: `var(--ui-color-neutral-900, #0f172b)`,
```

In **CardDoughnutChart.vue** line 77, replace:

```
borderColor: colorMode.value === "dark" ? "#0f172b" : undefined,
```

With:

```
borderColor: `var(--ui-color-neutral-900, #0f172b)`,
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: Build succeeds.

---

### Task 11: Verification

- [ ] **Step 1: Frontend build**

Run: `npm run build`
Expected: `✓ built in Xs`, no errors.

- [ ] **Step 2: Backend build + tests**

Run: `dotnet test`
Expected: All 28 tests pass.

---

### Task 12: Manual Testing

- [ ] **Step 1: Start both servers**

- [ ] **Step 2: Open app with company that has primary color set**
  - Check DevTools Application → IndexedDB → `keyval-store` → `chart-colors:emerald` exists
  - Refresh — should use cached value (no network request to `/api/theme/chart-colors`)

- [ ] **Step 3: Check API directly**

```
curl http://localhost:5165/api/theme/chart-colors?primaryColor=emerald
```
Expected: 8 hex values as JSON array.

- [ ] **Step 4: Verify NumberCard trend colors follow primary**
  - Company with emerald primary → up arrow = green, down arrow = emerald, flat = gray

- [ ] **Step 5: Verify border colors on doughnut/bar charts use neutral-900**
