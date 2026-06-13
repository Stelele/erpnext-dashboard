# Theme Mode Per Company — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove permanently-forced dark mode and add a per-company `ThemeMode` (Dark/Light) field to CompanySettings, stored alongside PrimaryColor and NeutralColor.

**Architecture:** Add a `ThemeMode` enum to the C# domain layer, thread it through the request/response DTOs, the EF entity config, and the upsert command handler. On the frontend, remove the hardcoded dark-mode forcing in `main.ts` and `style.css`, and apply the mode via the existing `useCompanyTheme` composable when company settings load. Null/undefined defaults to dark for backward compatibility.

**Tech Stack:** .NET 10, EF Core, Vue 3, Nuxt UI v4, Tailwind CSS

---

## File Structure

| File | Role |
|---|---|
| `backend/Domain/CompanySettings/ColorEnums.cs` | New `ThemeMode` enum |
| `backend/Domain/CompanySettings/CompanySettings.cs` | Domain entity — add property |
| `backend/Application/Requests/UpdateCompanySettingsRequest.cs` | API request DTO — add field |
| `backend/Application/DTOs/CompanySettingsResponse.cs` | API response DTO — add field |
| `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs` | Upsert handler — wire new field |
| `backend/Infrastructure/Models/CompanySettingsEntity.cs` | EF mapping — add column config |
| `frontend/src/main.ts` | Remove forced dark mode |
| `frontend/src/style.css` | Remove forced `color-scheme: dark` |
| `frontend/src/composables/useCompanyTheme.ts` | Apply mode based on settings |
| `frontend/src/types/Expenses.ts` | Add `themeMode` to interface |
| `frontend/src/services/api/schema.ts` | Add `ThemeMode` type to generated schema |

---

### Task 1: Add `ThemeMode` enum to domain layer

**Files:**
- Modify: `backend/Domain/CompanySettings/ColorEnums.cs`

- [ ] **Step 1: Add the enum**

Append to the file:

```csharp
public enum ThemeMode
{
    Dark,
    Light
}
```

- [ ] **Step 2: Build backend to verify compilation**

Run: `dotnet build backend/Api/Host/Host.csproj`
Expected: Build succeeds (no errors)

- [ ] **Step 3: Commit**

```bash
git add backend/Domain/CompanySettings/ColorEnums.cs
git commit -m "feat: add ThemeMode enum to domain layer"
```

---

### Task 2: Add `ThemeMode` to domain entity

**Files:**
- Modify: `backend/Domain/CompanySettings/CompanySettings.cs:10` (after `NeutralColor` line)

- [ ] **Step 1: Add property to entity**

Add after line 11 (`public NeutralColor? NeutralColor { get; set; }`):

```csharp
    public ThemeMode? ThemeMode { get; set; }
```

Full file after edit:

```csharp
using Domain.Abstractions;
using Domain.Companies;

namespace Domain.CompanySettings;

public class CompanySettings : Base
{
    public Guid CompanyId { get; set; }
    public string DefaultIncomeAccountName { get; set; } = "Sales";
    public PrimaryColor? PrimaryColor { get; set; }
    public NeutralColor? NeutralColor { get; set; }
    public ThemeMode? ThemeMode { get; set; }

    public Company Company { get; set; } = null!;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build backend/Api/Host/Host.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/Domain/CompanySettings/CompanySettings.cs
git commit -m "feat: add ThemeMode property to CompanySettings domain entity"
```

---

### Task 3: Add `ThemeMode` to update request DTO

**Files:**
- Modify: `backend/Application/Requests/UpdateCompanySettingsRequest.cs`

- [ ] **Step 1: Add field to record and validator**

Replace file content:

```csharp
using Domain.CompanySettings;
using FluentValidation;

namespace Application.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null
);

public class UpdateCompanySettingsRequestValidator : AbstractValidator<UpdateCompanySettingsRequest>
{
    public UpdateCompanySettingsRequestValidator()
    {
        RuleFor(x => x.DefaultIncomeAccountName).NotEmpty();
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build backend/Api/Host/Host.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/Application/Requests/UpdateCompanySettingsRequest.cs
git commit -m "feat: add ThemeMode to UpdateCompanySettingsRequest"
```

---

### Task 4: Add `ThemeMode` to response DTO

**Files:**
- Modify: `backend/Application/DTOs/CompanySettingsResponse.cs`

- [ ] **Step 1: Add field to record and FromDomain factory**

Replace file content:

```csharp
using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;

namespace Application.DTOs;

public record CompanySettingsResponse(
    Guid Id,
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor,
    NeutralColor? NeutralColor,
    ThemeMode? ThemeMode
)
{
    public static CompanySettingsResponse FromDomain(CompanySettingsEntity settings) =>
        new(
            settings.Id,
            settings.CompanyId,
            settings.DefaultIncomeAccountName,
            settings.PrimaryColor,
            settings.NeutralColor,
            settings.ThemeMode
        );
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build backend/Api/Host/Host.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/Application/DTOs/CompanySettingsResponse.cs
git commit -m "feat: add ThemeMode to CompanySettingsResponse DTO"
```

---

### Task 5: Add `ThemeMode` to EF entity configuration

**Files:**
- Modify: `backend/Infrastructure/Models/CompanySettingsEntity.cs:34` (after `NeutralColor` config)

- [ ] **Step 1: Add EF property mapping**

Add after line 35 (the `NeutralColor` `.HasMaxLength(50);` line):

```csharp

        builder
            .Property(e => e.ThemeMode)
            .HasConversion<string>()
            .HasMaxLength(50);
```

Full file after edit:

```csharp
using Domain.CompanySettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class CompanySettingsEntity : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings");

        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.DefaultIncomeAccountName)
            .IsRequired()
            .HasMaxLength(255);

        builder
            .Property(e => e.PrimaryColor)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder
            .Property(e => e.NeutralColor)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder
            .Property(e => e.ThemeMode)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder
            .HasIndex(e => e.CompanyId)
            .IsUnique();

        builder
            .Property(e => e.CompanyId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Generate EF Core migration**

Run: `dotnet ef migrations add AddThemeModeToCompanySettings --project backend/Api/Host/Host.csproj --startup-project backend/Api/Host/Host.csproj`

If the EF tool isn't installed globally, use:
`dotnet tool run dotnet-ef migrations add AddThemeModeToCompanySettings --project backend/Api/Host/Host.csproj --startup-project backend/Api/Host/Host.csproj`

Expected: Migration file created in `backend/Api/Host/Migrations/`

- [ ] **Step 3: Build to verify**

Run: `dotnet build backend/Api/Host/Host.csproj`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add backend/Infrastructure/Models/CompanySettingsEntity.cs backend/Api/Host/Migrations/
git commit -m "feat: add ThemeMode column to CompanySettings table with migration"
```

---

### Task 6: Wire `ThemeMode` into upsert command handler

**Files:**
- Modify: `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`

- [ ] **Step 1: Add ThemeMode to command record and handler**

Replace file content:

```csharp
using Application.Abstractions;
using Application.Caching;
using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

[InvalidateCache(Category = "settings")]
public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null
) : ICommand;

internal class UpdateCompanySettingsCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateCompanySettingsCommand>
{
    public async Task Handle(UpdateCompanySettingsCommand request, CancellationToken ct)
    {
        var settings = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        if (settings == null)
        {
            settings = new CompanySettingsEntity
            {
                CompanyId = request.CompanyId,
                DefaultIncomeAccountName = request.DefaultIncomeAccountName,
                PrimaryColor = request.PrimaryColor,
                NeutralColor = request.NeutralColor,
                ThemeMode = request.ThemeMode,
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
            settings.PrimaryColor = request.PrimaryColor;
            settings.NeutralColor = request.NeutralColor;
            settings.ThemeMode = request.ThemeMode;
            settings.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build backend/Api/Host/Host.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs
git commit -m "feat: wire ThemeMode into UpdateCompanySettingsCommand handler"
```

---

### Task 7: Apply backend migration to database

**Files:**
- None (database operation)

- [ ] **Step 1: Apply migration**

Run: `dotnet ef database update --project backend/Api/Host/Host.csproj --startup-project backend/Api/Host/Host.csproj`

If using local tool:
`dotnet tool run dotnet-ef database update --project backend/Api/Host/Host.csproj --startup-project backend/Api/Host/Host.csproj`

Expected: Migration applied successfully, no errors

- [ ] **Step 2: Verify column exists in database**

Check the database (PostgreSQL) that `CompanySettings` table now has a `ThemeMode` column of type `TEXT`:

Run: `psql -h localhost -U <user> -d <database> -c "\d \"CompanySettings\""`
Expected: Output includes `ThemeMode | text | |`

---

### Task 8: Remove forced dark mode from frontend bootstrap

**Files:**
- Modify: `frontend/src/main.ts:14-17`
- Modify: `frontend/src/style.css:4-7`

- [ ] **Step 1: Remove forced dark mode from main.ts**

Delete lines 14-17:

```typescript
// Force dark mode permanently
localStorage.setItem('vueuse-color-scheme', 'dark');
document.documentElement.classList.add('dark');
document.documentElement.classList.remove('light');
```

Full file after edit:

```typescript
import "./style.css";
import App from "./App.vue";
import { createApp } from "vue";
import { router } from "./routes";
import { createPinia } from "pinia";
import ui from "@nuxt/ui/vue-plugin";
import { addCollection } from "@iconify/vue";
import lucide from "@iconify-json/lucide/icons.json";
import { createAuth0 } from "@auth0/auth0-vue";
import { createHead } from "@unhead/vue/client";

addCollection(lucide);

const app = createApp(App);
const pinia = createPinia();
const head = createHead();

app.use(
  createAuth0({
    domain: import.meta.env.VITE_AUTH0_DOMAIN,
    clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
    cacheLocation: import.meta.env.DEV ? 'localstorage' : undefined,
    authorizationParams: {
      audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      redirect_uri: window.location.origin,
      scope: "openid profile email read:users read:sites read:companies read:expenses update:expenses",
    },
  }),
);

app.use(router);
app.use(ui);
app.use(pinia);
app.use(head);

app.mount("#app");
```

- [ ] **Step 2: Remove forced color-scheme from style.css**

Delete lines 4-7:

```css
/* Force dark mode permanently */
html {
    color-scheme: dark;
}
```

Full file after edit:

```css
@import "tailwindcss";
@import "@nuxt/ui";

/* Hide UHeader's built-in mobile menu button */
header button[aria-label="Open menu"] {
    display: none !important;
}
```

- [ ] **Step 3: Build frontend to verify**

Run: `npm run build`
Expected: Build succeeds (may show TypeScript errors for ThemeMode type — that's expected until Task 10)

- [ ] **Step 4: Commit**

```bash
git add frontend/src/main.ts frontend/src/style.css
git commit -m "feat: remove permanently-forced dark mode from frontend"
```

---

### Task 9: Add `ThemeMode` type to generated API schema

**Files:**
- Modify: `frontend/src/services/api/schema.ts`

- [ ] **Step 1: Add ThemeMode type declaration**

Add after line 207 (after `NeutralColor` type):

```typescript
export type ThemeMode = 'Dark' | 'Light'
```

- [ ] **Step 2: Add themeMode to CompanySettingsResponse**

In the `CompanySettingsResponse` schema object (line 227), add after `neutralColor` line (line 234):

```typescript
            themeMode?: ThemeMode | null;
```

Full block:

```typescript
        CompanySettingsResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            companyId: string;
            defaultIncomeAccountName: string;
            primaryColor?: PrimaryColor | null;
            neutralColor?: NeutralColor | null;
            themeMode?: ThemeMode | null;
        };
```

- [ ] **Step 3: Add themeMode to UpdateCompanySettingsRequest**

In the `UpdateCompanySettingsRequest` schema object (line 280), add after `neutralColor` line (line 283):

```typescript
            themeMode?: ThemeMode | null;
```

Full block:

```typescript
        UpdateCompanySettingsRequest: {
            defaultIncomeAccountName: string;
            primaryColor?: PrimaryColor | null;
            neutralColor?: NeutralColor | null;
            themeMode?: ThemeMode | null;
        };
```

- [ ] **Step 4: Build frontend to verify**

Run: `npm run build`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add frontend/src/services/api/schema.ts
git commit -m "feat: add ThemeMode type to generated API schema"
```

---

### Task 10: Add `themeMode` to frontend CompanySettings type

**Files:**
- Modify: `frontend/src/types/Expenses.ts:16-22`

- [ ] **Step 1: Add themeMode to CompanySettings interface**

Add after line 21 (`neutralColor?: NeutralColor | null;`):

```typescript
  themeMode?: ThemeMode | null;
```

Update the import on line 1 to include `ThemeMode`:

```typescript
import type { PrimaryColor, NeutralColor, ThemeMode } from '@/services/api/schema';
```

Full interface after edit:

```typescript
export interface CompanySettings {
  id: string;
  companyId: string;
  defaultIncomeAccountName: string;
  primaryColor?: PrimaryColor | null;
  neutralColor?: NeutralColor | null;
  themeMode?: ThemeMode | null;
}
```

- [ ] **Step 2: Build frontend to verify**

Run: `npm run build`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add frontend/src/types/Expenses.ts
git commit -m "feat: add themeMode to CompanySettings TypeScript interface"
```

---

### Task 11: Apply theme mode in useCompanyTheme composable

**Files:**
- Modify: `frontend/src/composables/useCompanyTheme.ts`

- [ ] **Step 1: Add applyThemeMode function and call it in loadAndApply**

Replace file content:

```typescript
import colors from 'tailwindcss/colors'
import type { PrimaryColor, NeutralColor, ThemeMode } from '@/services/api/schema'
import { useChartColors } from './useChartColors'

function applyPrimaryPalette(colorName: PrimaryColor | null | undefined): void {
  if (!colorName) {
    clearPalette('primary')
    return
  }
  if (colorName === 'black') {
    document.documentElement.style.setProperty('--ui-primary', 'black')
    for (let shade = 50; shade <= 950; shade += 50) {
      document.documentElement.style.removeProperty(`--ui-color-primary-${shade}`)
    }
    return
  }
  const palette = (colors as Record<string, Record<string, string> | string>)[colorName]
  if (typeof palette === 'object') {
    for (const [shade, value] of Object.entries(palette)) {
      document.documentElement.style.setProperty(`--ui-color-primary-${shade}`, value)
    }
  }
}

function applyNeutralPalette(colorName: NeutralColor | null | undefined): void {
  if (!colorName) {
    clearPalette('neutral')
    return
  }
  const palette = (colors as Record<string, Record<string, string> | string>)[colorName]
  if (typeof palette === 'object') {
    for (const [shade, value] of Object.entries(palette)) {
      document.documentElement.style.setProperty(`--ui-color-neutral-${shade}`, value)
    }
  }
  const neutral900 = palette['900'] as string | undefined
  if (neutral900) {
    const match = neutral900.match(/oklch\(([\d.]+)%\s+[\d.]+\s+[\d.]+\)/)
    if (match) {
      document.documentElement.style.setProperty('--ui-text-inverted', `oklch(${match[1]}% 0 0)`)
    }
  }
}

function applyThemeMode(mode: ThemeMode | null | undefined): void {
  if (mode === 'Light') {
    document.documentElement.classList.remove('dark')
    document.documentElement.style.colorScheme = 'light'
  } else {
    document.documentElement.classList.add('dark')
    document.documentElement.style.colorScheme = 'dark'
  }
}

function clearPalette(semanticName: 'primary' | 'neutral'): void {
  for (let shade = 50; shade <= 950; shade += 50) {
    document.documentElement.style.removeProperty(`--ui-color-${semanticName}-${shade}`)
  }
  if (semanticName === 'primary') {
    document.documentElement.style.removeProperty('--ui-primary')
  }
  if (semanticName === 'neutral') {
    document.documentElement.style.removeProperty('--ui-text-inverted')
  }
}

export function useCompanyTheme() {
  const { loadChartColors } = useChartColors()

  async function loadAndApply(companyId: string): Promise<void> {
    const { useDataStore } = await import('@/stores/DataStore')
    const dataStore = useDataStore()
    const settings = await dataStore.getCompanySettings(companyId)

    applyPrimaryPalette(settings?.primaryColor ?? undefined)
    applyNeutralPalette(settings?.neutralColor ?? undefined)
    applyThemeMode(settings?.themeMode ?? undefined)
    await loadChartColors(settings?.primaryColor ?? undefined)
  }

  return { loadAndApply }
}
```

- [ ] **Step 2: Build frontend to verify**

Run: `npm run build`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add frontend/src/composables/useCompanyTheme.ts
git commit -m "feat: apply theme mode (dark/light) from company settings"
```

---

### Task 12: End-to-end verification

**Files:**
- None (manual verification)

- [ ] **Step 1: Build both projects**

Run: `dotnet build backend/Api/Host/Host.csproj && npm run build`
Expected: Both succeed

- [ ] **Step 2: Start backend and test via curl**

Run backend (in one terminal):
`dotnet run --project backend/Api/Host/Host.csproj`

In another terminal, test:
```bash
# Update settings to light mode
curl -X PUT http://localhost:5000/api/companies/<company-id>/settings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"defaultIncomeAccountName":"Sales","themeMode":"Light"}'

# Verify the response includes themeMode
curl http://localhost:5000/api/companies/<company-id>/settings \
  -H "Authorization: Bearer <token>"
```

Expected: Response includes `"themeMode":"Light"`

- [ ] **Step 3: Lint frontend**

Run: `npm run lint` (or equivalent lint command)
Expected: No new lint errors

- [ ] **Step 4: Commit any remaining changes**

```bash
git status
git add <any remaining files>
git commit -m "chore: final verification of theme mode feature"
```
