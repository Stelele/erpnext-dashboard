# Company Theme Colors — Design Spec

**Date:** 2026-06-12
**Status:** Approved

## Overview

Add per-company branding via Nuxt UI's semantic color system. Companies have a **primary** color and a **neutral** palette — stored in the backend database, applied at runtime through CSS custom properties. No visible color picker in the app UI; colors are set via REST API.

## Valid Color Values

Matches the Nuxt UI site's built-in color picker exactly.

### Primary (18 options)

`black`, `red`, `orange`, `amber`, `yellow`, `lime`, `green`, `emerald`, `teal`, `cyan`, `sky`, `blue`, `indigo`, `violet`, `purple`, `fuchsia`, `pink`, `rose`

- `black` is a special case: has no shades (50–950). Instead, `--ui-primary: black` is set directly per Nuxt UI docs.

### Neutral (9 options)

`slate`, `gray`, `zinc`, `neutral`, `stone`, `taupe`, `mauve`, `mist`, `olive`

## Affected CSS Variables

| Semantic Color | CSS Variables Set | What It Affects |
|---|---|---|
| Primary | `--ui-color-primary-50` through `--ui-color-primary-950`, `--ui-primary` | All `color="primary"` components: buttons, badges, links, active nav, focus rings |
| Neutral | `--ui-color-neutral-50` through `--ui-color-neutral-950` | `--ui-bg`, `--ui-bg-muted`, `--ui-bg-elevated`, `--ui-text`, `--ui-border` — entire app shell, backgrounds, text tones, borders, cards, sidebar |

## Backend Changes

### Enums: `Domain/CompanySettings/`

New file `backend/Domain/CompanySettings/ColorEnums.cs`:

```csharp
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrimaryColor
{
    Black, Red, Orange, Amber, Yellow, Lime, Green, Emerald,
    Teal, Cyan, Sky, Blue, Indigo, Violet, Purple, Fuchsia, Pink, Rose
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NeutralColor
{
    Slate, Gray, Zinc, Neutral, Stone, Taupe, Mauve, Mist, Olive
}
```

- `JsonStringEnumConverter` serializes C# `Emerald` → JSON `"emerald"` and deserializes `"blue"` → C# `Blue` automatically.
- Invalid values (e.g. `"magenta"`) fail at JSON deserialization → free 400 Bad Request with no manual validation code.
- The JSON string value matches `tailwindcss/colors` keys exactly: `colors.emerald`, `colors.blue`, etc.

### Model: `CompanySettings` (`backend/Domain/CompanySettings/CompanySettings.cs`)

Add two properties:

```csharp
public PrimaryColor? PrimaryColor { get; set; }
public NeutralColor? NeutralColor { get; set; }
```

- Both nullable — null means "use Nuxt UI default" (green primary, slate neutral).
- EF Core stores enums as strings in the database by default (the JSON name).

### DTO: `CompanySettingsResponse` (`backend/Application/DTOs/CompanySettingsResponse.cs`)

Add matching nullable enum properties (serialized as lowercase strings in JSON).

### Request: `UpdateCompanySettingsRequest` (`backend/Application/Requests/UpdateCompanySettingsRequest.cs`)

Add `PrimaryColor?` and `NeutralColor?` as optional. Invalid values rejected automatically by the JSON serializer — no manual validation needed.

### Command: `UpdateCompanySettingsCommand`

Map the new fields through to the domain model (already uses upsert pattern).

### EF Core Configuration

In `CompanySettingsEntity.cs`, configure enum-to-string conversion:

```csharp
builder.Property(cs => cs.PrimaryColor).HasConversion<string>();
builder.Property(cs => cs.NeutralColor).HasConversion<string>();
```

### Migration

New EF Core migration adding `PrimaryColor` (TEXT) and `NeutralColor` (TEXT) columns to `CompanySettings` table.

## Frontend Changes

### TypeScript types (mirror backend enums)

In the API schema (`frontend/src/services/api/schema.ts`), the settings types become:

```ts
type PrimaryColor = 'black' | 'red' | 'orange' | 'amber' | 'yellow' | 'lime' | 'green' | 'emerald' | 'teal' | 'cyan' | 'sky' | 'blue' | 'indigo' | 'violet' | 'purple' | 'fuchsia' | 'pink' | 'rose'

type NeutralColor = 'slate' | 'gray' | 'zinc' | 'neutral' | 'stone' | 'taupe' | 'mauve' | 'mist' | 'olive'
```

These string literal unions are the JSON-deserialized form of the C# enums. The TypeScript type checker enforces they match the valid set.

### Palette application

New composable at `frontend/src/composables/useCompanyTheme.ts`:

```ts
import colors from 'tailwindcss/colors'
import type { PrimaryColor, NeutralColor } from '@/services/api/schema'

function applyPalette(colorName: PrimaryColor, semanticName: 'primary'): void
function applyPalette(colorName: NeutralColor, semanticName: 'neutral'): void
function applyPalette(colorName: string | null | undefined, semanticName: 'primary' | 'neutral') {
  if (!colorName) {
    clearPalette(semanticName)
    return
  }
  if (colorName === 'black' && semanticName === 'primary') {
    document.documentElement.style.setProperty('--ui-primary', 'black')
    for (let shade = 50; shade <= 950; shade += 50) {
      document.documentElement.style.removeProperty(`--ui-color-primary-${shade}`)
    }
    return
  }
  const palette = colors[colorName as keyof typeof colors]
  for (const [shade, value] of Object.entries(palette)) {
    document.documentElement.style.setProperty(`--ui-color-${semanticName}-${shade}`, String(value))
  }
}

function clearPalette(semanticName: 'primary' | 'neutral') {
  for (let shade = 50; shade <= 950; shade += 50) {
    document.documentElement.style.removeProperty(`--ui-color-${semanticName}-${shade}`)
  }
  document.documentElement.style.removeProperty(`--ui-${semanticName}`)
}
```

The JSON enum string value (`"emerald"`) is the exact key in `tailwindcss/colors` — zero transformation needed.

### Integration point

Called from `App.vue` (or auth/data store) after company settings are fetched:

```
onCompanyLoad(companyId) →
  GET /api/companies/{companyId}/settings →
  applyPalette(response.primaryColor, 'primary')  // null → clears to default
  applyPalette(response.neutralColor, 'neutral')  // null → clears to default
```

Also called on company switch (existing switch-organization flow).

### API schema update

`frontend/src/services/api/schema.ts` — add the `PrimaryColor` and `NeutralColor` type aliases and add them as nullable fields on the settings request/response types. The `openapi-fetch` client will then enforce these types on API calls.

## Zero New Dependencies

`tailwindcss/colors` is already installed as part of `tailwindcss`. No additional packages.

## Scope Boundaries

| In Scope | Out of Scope |
|---|---|
| Primary + neutral palette from named Tailwind colors | Custom hex colors |
| Per-company branding via REST API | Admin UI to set colors |
| Dark mode only (app is dark-only) | Light mode |
| Apply to all Nuxt UI `color` props and shell | Chart colors (separate `chartJsColors` concern) |
| Company switch triggers re-apply | User-level theme preferences |
