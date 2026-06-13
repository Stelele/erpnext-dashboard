# Theme Mode Per Company — Design Spec

**Date:** 2026-06-13  
**Status:** Draft

## Problem

Dark mode is permanently forced (`main.ts` hardcodes `localStorage.setItem('vueuse-color-scheme', 'dark')` and adds `dark` class to `<html>`, and `style.css` forces `color-scheme: dark`). There is no way for companies to choose light mode.

## Goal

Remove the permanently-forced dark theme and add a per-company `ThemeMode` (`Dark` / `Light`) setting stored alongside `PrimaryColor` and `NeutralColor` in `CompanySettings`.

## Non-Goals

- No Theme Builder UI (color pickers, live preview, etc.)
- No per-user preference (per-company only)
- No changes to chart color generation

---

## Design

### Backend

#### New enum (`Domain/CompanySettings/ColorEnums.cs`)

```csharp
public enum ThemeMode
{
    Dark,
    Light
}
```

#### Domain entity (`Domain/CompanySettings/CompanySettings.cs`)

Add nullable property:
```csharp
public ThemeMode? ThemeMode { get; set; }
```

#### Request DTO (`Application/Requests/UpdateCompanySettingsRequest.cs`)

Add nullable property:
```csharp
public ThemeMode? ThemeMode { get; set; }
```

#### Response DTO (`Application/DTOs/CompanySettingsResponse.cs`)

Add nullable property:
```csharp
public ThemeMode? ThemeMode { get; set; }
```

#### EF configuration (`Infrastructure/Models/CompanySettingsEntity.cs`)

Map new column as `TEXT`, nullable, max 50 chars — same pattern as `PrimaryColor` and `NeutralColor`.

#### Migration

New EF Core migration adding `ThemeMode TEXT NULL` column to `CompanySettings` table.

#### Create handler (`Application/Companies/CreateCompanyCommandHandler.cs`)

Seed default `CompanySettings` with `ThemeMode = null` (same as existing — no theme colors set initially).

#### Update handler (`Application/Companies/UpdateCompanySettingsCommandHandler.cs`)

Already upserts — the new field flows through automatically via request mapping.

---

### Frontend

#### Remove forced dark mode (`frontend/src/main.ts`)

Remove these 3 lines:
```typescript
localStorage.setItem('vueuse-color-scheme', 'dark');
document.documentElement.classList.add('dark');
document.documentElement.classList.remove('light');
```

#### Remove forced CSS (`frontend/src/style.css`)

Remove:
```css
html { color-scheme: dark; }
```

#### Theme composable (`frontend/src/composables/useCompanyTheme.ts`)

Inside `loadAndApply(companyId)`, after loading settings and applying colors, add mode logic:

```
if (settings.themeMode === 'Light')
    → document.documentElement.classList.remove('dark')
    → document.documentElement.style.colorScheme = 'light'
else (Dark or null)
    → document.documentElement.classList.add('dark')
    → document.documentElement.style.colorScheme = 'dark'
```

#### TypeScript types

Add to `CompanySettings` interface:
```typescript
themeMode?: 'Dark' | 'Light' | null;
```

#### API schema (`frontend/src/services/api/schema.ts`)

Regenerate or manually add `themeMode` field to `CompanySettingsResponse` and `UpdateCompanySettingsRequest`.

---

### Edge Cases & Error Handling

| Scenario | Behavior |
|---|---|
| New company created | `ThemeMode = null` seeded → frontend treats as **dark** |
| Existing company with null ThemeMode | Frontend falls back to dark (backward compatible) |
| Company switch | `CompanySwitcherModalContent` re-calls `loadAndApply()` — picks up mode automatically |
| CSS flash on load | Mode applied in `App.vue:onBeforeMount` — no visible flash |
| Chart components | Use VueUse `useColorMode()` which reads the `dark` class on `<html>` — updates automatically |

---

## Files Changed

### Backend
| File | Change |
|---|---|
| `backend/Domain/CompanySettings/ColorEnums.cs` | Add `ThemeMode` enum |
| `backend/Domain/CompanySettings/CompanySettings.cs` | Add `ThemeMode?` property |
| `backend/Application/Requests/UpdateCompanySettingsRequest.cs` | Add `ThemeMode?` property |
| `backend/Application/DTOs/CompanySettingsResponse.cs` | Add `ThemeMode?` property |
| `backend/Infrastructure/Models/CompanySettingsEntity.cs` | Map new column |
| `backend/Application/Companies/CreateCompanyCommandHandler.cs` | Seed default (no change, already null) |
| Migration (auto-generated) | Add column |

### Frontend
| File | Change |
|---|---|
| `frontend/src/main.ts` | Remove forced dark mode lines |
| `frontend/src/style.css` | Remove forced `color-scheme: dark` |
| `frontend/src/composables/useCompanyTheme.ts` | Add `applyThemeMode()` |
| `frontend/src/types/Expenses.ts` | Add `themeMode` to interface |
| `frontend/src/services/api/schema.ts` | Add `themeMode` to generated types |
