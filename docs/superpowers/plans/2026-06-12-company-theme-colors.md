# Company Theme Colors Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-company primary and neutral color branding via Nuxt UI's semantic color system, stored as named palette references in the database.

**Architecture:** Backend stores C# enums (Persisted to DB as strings). Frontend composable maps enum string values to `tailwindcss/colors` palettes and sets `--ui-color-primary-*` / `--ui-color-neutral-*` CSS custom properties. Nuxt UI components consume these automatically.

**Tech Stack:** C# enums with `JsonStringEnumConverter`, EF Core string conversion, Tailwind CSS v4 `tailwindcss/colors`, CSS custom properties.

---

### Task 1: Backend — Color Enums

**Files:**
- Create: `backend/Domain/CompanySettings/ColorEnums.cs`

- [ ] **Step 1: Create the enum file**

```csharp
using System.Text.Json.Serialization;

namespace Domain.CompanySettings;

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

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build`
Expected: Build succeeds with new enums available.

---

### Task 2: Backend — Domain Model

**Files:**
- Modify: `backend/Domain/CompanySettings/CompanySettings.cs`

- [ ] **Step 1: Add color properties to CompanySettings**

Replace the file content:

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

    public Company Company { get; set; } = null!;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 3: Backend — DTO

**Files:**
- Modify: `backend/Application/DTOs/CompanySettingsResponse.cs`

- [ ] **Step 1: Add color fields to the response record**

Replace the file content:

```csharp
using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;

namespace Application.DTOs;

public record CompanySettingsResponse(
    Guid Id,
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor,
    NeutralColor? NeutralColor
)
{
    public static CompanySettingsResponse FromDomain(CompanySettingsEntity settings) =>
        new(
            settings.Id,
            settings.CompanyId,
            settings.DefaultIncomeAccountName,
            settings.PrimaryColor,
            settings.NeutralColor
        );
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 4: Backend — Request & Validation

**Files:**
- Modify: `backend/Application/Requests/UpdateCompanySettingsRequest.cs`

- [ ] **Step 1: Add optional color fields to the request record**

Replace the file content:

```csharp
using Domain.CompanySettings;
using FluentValidation;

namespace Application.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null
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

Run: `dotnet build`
Expected: Build succeeds. (Invalid enum values fail at `System.Text.Json` deserialization — no manual validation needed.)

---

### Task 5: Backend — Command & Handler

**Files:**
- Modify: `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`

- [ ] **Step 1: Add color fields to command and map in handler**

Replace the file content:

```csharp
using Application.Abstractions;
using Domain.CompanySettings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.CompanySettings;

public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null
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
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
            settings.PrimaryColor = request.PrimaryColor;
            settings.NeutralColor = request.NeutralColor;
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 6: Backend — Endpoint Wiring

**Files:**
- Modify: `backend/Endpoints/Endpoints/ExpenseEndpoints.cs`

- [ ] **Step 1: Pass new fields when constructing command**

In the `MapPut` handler at line 54–63, replace the command construction:

```csharp
// Old:
var command = new UpdateCompanySettingsCommand(companyId, request.DefaultIncomeAccountName);

// New:
var command = new UpdateCompanySettingsCommand(
    companyId,
    request.DefaultIncomeAccountName,
    request.PrimaryColor,
    request.NeutralColor
);
```

The full updated method should read:

```csharp
group.MapPut("/companies/{companyId:guid}/settings", async (Guid companyId, UpdateCompanySettingsRequest request, IMediator mediator) =>
    {
        var command = new UpdateCompanySettingsCommand(
            companyId,
            request.DefaultIncomeAccountName,
            request.PrimaryColor,
            request.NeutralColor
        );
        await mediator.Send(command);
        return Results.NoContent();
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("UpdateCompanySettings")
    .RequireAuthorization(Permissions.UpdateExpenses);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 7: Backend — EF Core Configuration

**Files:**
- Modify: `backend/Infrastructure/Models/CompanySettingsEntity.cs`

- [ ] **Step 1: Add enum-to-string conversions for color properties**

Add after the `DefaultIncomeAccountName` configuration (after line 26):

```csharp
        builder
            .Property(e => e.PrimaryColor)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder
            .Property(e => e.NeutralColor)
            .HasConversion<string>()
            .HasMaxLength(50);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 8: Backend — EF Core Migration

**Files:**
- Create: Migration file (auto-generated by EF Core)
- Modify: `backend/Infrastructure/Migrations/` (new migration added)

- [ ] **Step 1: Create the migration**

Run: `dotnet ef migrations add AddCompanyThemeColors --project backend/Host/Host.csproj --startup-project backend/Host/Host.csproj`
Expected: Migration file created with `PrimaryColor` and `NeutralColor` TEXT columns.

- [ ] **Step 2: Run the migration**

Run: `dotnet ef database update --project backend/Host/Host.csproj --startup-project backend/Host/Host.csproj`
Expected: Columns added to CompanySettings table.

---

### Task 9: Backend — Update Existing Tests

**Files:**
- Modify: `backend/Tests/ExpenseTests.cs`

- [ ] **Step 1: Update the UpdateAndGetCompanySettings test to include color fields**

In `UpdateAndGetCompanySettings` (line 129–143), update the request and assertion:

```csharp
[Fact]
public async Task UpdateAndGetCompanySettings()
{
    await ResetAsync();
    var companyId = await CreateCompanyAsync();

    var updateRequest = new UpdateCompanySettingsRequest(
        "Custom Income Account",
        Domain.CompanySettings.PrimaryColor.Blue,
        Domain.CompanySettings.NeutralColor.Stone
    );
    var updateResponse = await _client.PutAsJsonAsync($"/api/companies/{companyId}/settings", updateRequest);
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

    var getResponse = await _client.GetAsync($"/api/companies/{companyId}/settings");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    var settings = await getResponse.Content.ReadFromJsonAsync<CompanySettingsResponse>();
    Assert.NotNull(settings);
    Assert.Equal("Custom Income Account", settings.DefaultIncomeAccountName);
    Assert.Equal(Domain.CompanySettings.PrimaryColor.Blue, settings.PrimaryColor);
    Assert.Equal(Domain.CompanySettings.NeutralColor.Stone, settings.NeutralColor);
}
```

Add import at top of file:
```csharp
using Domain.CompanySettings;
```

- [ ] **Step 2: Run tests to verify**

Run: `dotnet test`
Expected: All tests pass, including `UpdateAndGetCompanySettings`.

---

### Task 10: Frontend — API Schema Types

**Files:**
- Modify: `frontend/src/services/api/schema.ts`

- [ ] **Step 1: Add PrimaryColor and NeutralColor type aliases**

Add before the `components` block (around line 200):

```ts
export type PrimaryColor = 'black' | 'red' | 'orange' | 'amber' | 'yellow' | 'lime' | 'green' | 'emerald' | 'teal' | 'cyan' | 'sky' | 'blue' | 'indigo' | 'violet' | 'purple' | 'fuchsia' | 'pink' | 'rose'

export type NeutralColor = 'slate' | 'gray' | 'zinc' | 'neutral' | 'stone' | 'taupe' | 'mauve' | 'mist' | 'olive'
```

- [ ] **Step 2: Update CompanySettingsResponse schema component (line 207–213)**

Replace:
```ts
        CompanySettingsResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            companyId: string;
            defaultIncomeAccountName: string;
        };
```

With:
```ts
        CompanySettingsResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            companyId: string;
            defaultIncomeAccountName: string;
            primaryColor?: PrimaryColor | null;
            neutralColor?: NeutralColor | null;
        };
```

- [ ] **Step 3: Update UpdateCompanySettingsRequest schema component (line 255–257)**

Replace:
```ts
        UpdateCompanySettingsRequest: {
            defaultIncomeAccountName: string;
        };
```

With:
```ts
        UpdateCompanySettingsRequest: {
            defaultIncomeAccountName: string;
            primaryColor?: PrimaryColor | null;
            neutralColor?: NeutralColor | null;
        };
```

- [ ] **Step 4: Build to verify**

Run: `npm run build` (or `npx vue-tsc --noEmit`)
Expected: No type errors.

---

### Task 11: Frontend — TypeScript Interface Update

**Files:**
- Modify: `frontend/src/types/Expenses.ts`

- [ ] **Step 1: Add color fields to CompanySettings interface**

Replace the interface on line 14–18:

```ts
import type { PrimaryColor, NeutralColor } from '@/services/api/schema';

export interface CompanySettings {
  id: string;
  companyId: string;
  defaultIncomeAccountName: string;
  primaryColor?: PrimaryColor | null;
  neutralColor?: NeutralColor | null;
}
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: No type errors.

---

### Task 12: Frontend — Composable

**Files:**
- Create: `frontend/src/composables/useCompanyTheme.ts`

- [ ] **Step 1: Create the composable**

```ts
import colors from 'tailwindcss/colors'
import type { PrimaryColor, NeutralColor } from '@/services/api/schema'

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
}

function clearPalette(semanticName: 'primary' | 'neutral'): void {
  for (let shade = 50; shade <= 950; shade += 50) {
    document.documentElement.style.removeProperty(`--ui-color-${semanticName}-${shade}`)
  }
  if (semanticName === 'primary') {
    document.documentElement.style.removeProperty('--ui-primary')
  }
}

export function useCompanyTheme() {
  async function loadAndApply(companyId: string): Promise<void> {
    const { useDataStore } = await import('@/stores/DataStore')
    const dataStore = useDataStore()
    const settings = await dataStore.getCompanySettings(companyId)

    applyPrimaryPalette(settings?.primaryColor ?? undefined)
    applyNeutralPalette(settings?.neutralColor ?? undefined)
  }

  return { loadAndApply }
}
```

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: No type errors.

---

### Task 13: Frontend — Integration on App Load

**Files:**
- Modify: `frontend/src/App.vue`

- [ ] **Step 1: Apply theme after auth store updates**

Update the `onBeforeMount` to call theme after auth loads:

```vue
<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";
import { useCompanyTheme } from "./composables/useCompanyTheme";
import moment from "moment";

moment.updateLocale("en", {
    week: {
        dow: 1,
    },
});

const authStore = useAuthStore();
const { loadAndApply } = useCompanyTheme();

onBeforeMount(async () => {
    await authStore.update();
    update();

    // Apply company brand colors from settings
    const currentCompany = authStore.companies.find(
        (c) => c.name === authStore.company
    );
    if (currentCompany) {
        await loadAndApply(currentCompany.id);
    }
});
</script>
```

> **Note:** The company ID comes from `companies.value[id]` Response. The authStore's `companies` array contains `CompanyResponse` objects with `id`. We find by `company` name (computed from selected or first).

- [ ] **Step 2: Build to verify**

Run: `npm run build`
Expected: No type errors.

---

### Task 14: Frontend — Integration on Company Switch

**Files:**
- Modify: `frontend/src/components/CompanySwitcherModalContent.vue`

- [ ] **Step 1: Add theme re-apply after switching**

Add the import at the top of `<script setup>` (after line 44):

```ts
import { useCompanyTheme } from "@/composables/useCompanyTheme";
```

Inside the script body (after `toast` declaration, before `selectCompany`), add:

```ts
const { loadAndApply } = useCompanyTheme();
```

In `selectCompany`, after the successful switch block, re-apply theme. The updated function:

```ts
async function selectCompany(companyName: string) {
    if (companyName === authStore.company) {
        isOpen.value = false;
        return;
    }

    dataStore.loading = true;
    dataStore.clear();
    try {
        await authStore.switchCompany(companyName, async () => {
            await dataStore.update();
        });

        // Re-apply brand theme for the new company
        const newCompany = authStore.companies.find((c) => c.name === companyName);
        if (newCompany) {
            await loadAndApply(newCompany.id);
        }

        isOpen.value = false;
        toast.add({
            title: `Switched to ${companyName}`,
            color: "success",
        });
    } catch {
        toast.add({
            title: `Failed to switch to ${companyName}`,
            color: "error",
        });
    } finally {
        dataStore.loading = false;
    }
}
```

- [ ] **Step 2: Check the full file for consistency**

Read the full file to ensure the `selectedCompany` ref doesn't conflict with an existing one. Then:

Run: `npm run build`
Expected: No type errors.

---

### Task 15: Backend — Verify Full Test Suite

- [ ] **Step 1: Run all tests**

Run: `dotnet test`
Expected: All tests pass (including the updated `UpdateAndGetCompanySettings` with color fields).

---

### Task 16: Frontend — Manual Verification

- [ ] **Step 1: Start the development server**

Run: `npm run dev`

- [ ] **Step 2: Verify default (no colors)**
  - Open the app, log in. Buttons and UI should use Nuxt UI defaults (green primary, slate neutral).
  - Use DevTools to inspect `document.documentElement.style` — no `--ui-color-primary-*` or `--ui-color-neutral-*` overrides.

- [ ] **Step 3: Set colors via API**

Use curl or Postman:
```bash
curl -X PUT http://localhost:PORT/api/companies/{companyId}/settings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"defaultIncomeAccountName":"Sales","primaryColor":"emerald","neutralColor":"zinc"}'
```

- [ ] **Step 4: Refresh and verify**
  - Buttons, active nav, focus rings should use emerald.
  - Backgrounds, text tones, borders should use zinc.
  - DevTools should show `--ui-color-primary-500: oklch(72.3% 0.219 149.579)` (emerald) etc.

- [ ] **Step 5: Test "black" primary special case**

```bash
curl -X PUT ... -d '{"defaultIncomeAccountName":"Sales","primaryColor":"black","neutralColor":"slate"}'
```
Expected: `--ui-color-primary-*` shades cleared, `--ui-primary: black` set.

- [ ] **Step 6: Switch companies**
  - Company A with emerald/zinc
  - Company B with rose/slate
  - Switching should re-apply the correct palette each time.

---

### Task 17: Commit

- [ ] **Step 1: Stage and commit all changes**

```bash
git add backend/Domain/CompanySettings/ColorEnums.cs
git add backend/Domain/CompanySettings/CompanySettings.cs
git add backend/Application/DTOs/CompanySettingsResponse.cs
git add backend/Application/Requests/UpdateCompanySettingsRequest.cs
git add backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs
git add backend/Endpoints/Endpoints/ExpenseEndpoints.cs
git add backend/Infrastructure/Models/CompanySettingsEntity.cs
git add backend/Infrastructure/Migrations/
git add backend/Tests/ExpenseTests.cs
git add frontend/src/services/api/schema.ts
git add frontend/src/types/Expenses.ts
git add frontend/src/composables/useCompanyTheme.ts
git add frontend/src/App.vue
git add frontend/src/components/CompanySwitcherModalContent.vue
git commit -m "feat: add per-company primary and neutral theme colors"
```
