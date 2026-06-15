# Mobile-Friendly Purchase Modal — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the purchase modal mobile-friendly on Samsung A50 (412px viewport) using card-based item layout with no horizontal scroll.

**Architecture:** Responsive split using `hidden md:block` / `md:hidden` Tailwind classes within `PurchaseForm.vue`. Desktop keeps the existing 6-column grid. Mobile renders items as `UCard` components with stacked fields. Modal goes fullscreen on mobile via `:fullscreen` prop. No backend changes, no logic changes.

**Tech Stack:** Vue 3, Nuxt UI v4.8.2, Tailwind CSS

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `frontend/src/components/PurchaseForm.vue` | Modify | Add responsive mobile layout, keep desktop grid |
| `frontend/src/views/StockView.vue` | Modify | Add `isMobile` detection, `:fullscreen` on UModal |

---

### Task 1: Add Mobile Detection to StockView

**Files:**
- Modify: `frontend/src/views/StockView.vue`

- [ ] **Step 1: Add `onMounted`, `onUnmounted` to vue import**

Edit `frontend/src/views/StockView.vue`, change line 59 from:
```ts
import { ref, computed } from "vue";
```
To:
```ts
import { ref, computed, onMounted, onUnmounted } from "vue";
```

- [ ] **Step 2: Add `isMobile` ref and media query listener with cleanup**

Insert after `const openPurchase = ref(false);` (after line 65):
```ts
const isMobile = ref(false);

function syncMobile(mq: MediaQueryList | MediaQueryListEvent) {
  isMobile.value = mq.matches;
}

onMounted(() => {
  const mq = window.matchMedia('(max-width: 767px)');
  syncMobile(mq);
  mq.addEventListener('change', syncMobile);
  onUnmounted(() => mq.removeEventListener('change', syncMobile));
});
```

- [ ] **Step 3: Add `:fullscreen` to UModal**

Change the `UModal` opening tag (lines 4–8) from:
```html
<UModal
    v-model:open="openPurchase"
    title="Quick Purchase Entry"
    :dismissible="false"
    :ui="{ content: 'sm:max-w-2xl' }"
>
```
To:
```html
<UModal
    v-model:open="openPurchase"
    title="Quick Purchase Entry"
    :dismissible="false"
    :fullscreen="isMobile"
    :ui="{ content: 'sm:max-w-2xl' }"
>
```

- [ ] **Step 4: Build and verify no TS errors**

Run: `npm run build` in `frontend/`
Expected: Build succeeds.

---

### Task 2: Add Responsive Split to PurchaseForm Template

**Files:**
- Modify: `frontend/src/components/PurchaseForm.vue`

- [ ] **Step 1: Restructure the entry-form template**

Replace the entire `v-if="!showConfirm"` block (lines 3–85) with the code below. The confirmation block (`v-else`, lines 87–142) remains unchanged.

```html
    <div v-if="!showConfirm">
    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">

      <!-- Desktop: current 2-col header + 6-col items grid -->
      <div class="hidden md:block space-y-4">
        <div class="grid grid-cols-2 gap-4">
          <UFormField label="Supplier" name="supplier" required>
            <UInputMenu
              v-model="selectedSupplier as any"
              :items="supplierItems"
              value-key="name"
              label-key="supplier_name"
              placeholder="Search supplier..."
              class="w-full"
              :disabled="submitting"
            />
          </UFormField>
          <UFormField label="Warehouse" name="warehouse" required>
            <UInputMenu
              v-model="selectedWarehouse as any"
              :items="warehouseOpts"
              value-key="name"
              label-key="name"
              class="w-full"
              :disabled="submitting"
            />
          </UFormField>
          <UFormField label="Invoice No.">
            <UInput v-model="state.invoiceNumber" placeholder="Optional" class="w-full" :disabled="submitting" />
          </UFormField>
          <UFormField label="Invoice Date">
            <UPopover>
              <UButton color="neutral" variant="subtle" icon="i-lucide-calendar" class="w-full" :disabled="submitting">
                {{ displayDate }}
              </UButton>
              <template #content>
                <UCalendar v-model="state.invoiceDate" class="p-2" />
              </template>
            </UPopover>
          </UFormField>
        </div>

        <div>
          <label class="text-sm font-medium mb-2 block">Items</label>
          <div class="grid grid-cols-[2fr_1fr_1fr_1fr_100px_auto] gap-2 mb-2 text-xs font-medium text-[var(--ui-text-dimmed)] px-1">
            <span>Product</span>
            <span>Qty</span>
            <span>Buy Rate</span>
            <span>Sell Rate</span>
            <span>Total Buy</span>
            <span></span>
          </div>
          <div v-for="(item, idx) in state.items" :key="idx" class="grid grid-cols-[2fr_1fr_1fr_1fr_100px_auto] gap-2 mb-2">
            <UInputMenu
              v-model="itemSelections[idx] as any"
              v-model:search-term="itemSearchTerms[idx]"
              :items="itemOpts[idx]"
              value-key="item_code"
              label-key="item_name"
              description-key="description"
              :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
              class="w-full"
              :disabled="submitting"
              @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
              @update:model-value="() => onItemPicked(idx)"
            />
            <UInput v-model="item.qty" type="number" :min="1" :step="1" class="w-full" :disabled="submitting" />
            <UInput v-model="item.rate" type="number" :min="0" :step="0.01" class="w-full" :disabled="submitting" />
            <UInput v-model="item.sell_rate" type="number" :min="0" :step="0.01" class="w-full" :disabled="submitting" />
            <div class="flex items-center justify-end text-sm font-medium">
              {{ ((item.qty || 0) * (item.rate || 0)).toFixed(2) }}
            </div>
            <UButton color="error" variant="ghost" icon="i-lucide-x" size="sm" :disabled="submitting" @click="removeItem(idx)" />
          </div>
          <UButton variant="outline" icon="i-lucide-plus" size="sm" class="w-full" :disabled="submitting" @click="addItem">
            Add Item
          </UButton>
        </div>
      </div>

      <!-- Mobile: stacked single-column + UCards for items -->
      <div class="md:hidden space-y-4">
        <UFormField label="Supplier" name="supplier" required size="xs">
          <UInputMenu
            v-model="selectedSupplier as any"
            :items="supplierItems"
            value-key="name"
            label-key="supplier_name"
            placeholder="Search supplier..."
            class="w-full"
            :disabled="submitting"
          />
        </UFormField>
        <UFormField label="Warehouse" name="warehouse" required size="xs">
          <UInputMenu
            v-model="selectedWarehouse as any"
            :items="warehouseOpts"
            value-key="name"
            label-key="name"
            class="w-full"
            :disabled="submitting"
          />
        </UFormField>
        <UFormField label="Invoice No." size="xs">
          <UInput v-model="state.invoiceNumber" placeholder="Optional" class="w-full" :disabled="submitting" />
        </UFormField>
        <UFormField label="Invoice Date" size="xs">
          <UPopover>
            <UButton color="neutral" variant="subtle" icon="i-lucide-calendar" class="w-full justify-start" :disabled="submitting">
              {{ displayDate }}
            </UButton>
            <template #content>
              <UCalendar v-model="state.invoiceDate" class="p-2" />
            </template>
          </UPopover>
        </UFormField>

        <div>
          <div class="flex items-center justify-between mb-3">
            <span class="text-sm font-medium">Items</span>
            <UButton variant="outline" icon="i-lucide-plus" size="xs" :disabled="submitting" @click="addItem">Add</UButton>
          </div>

          <div class="space-y-3">
            <UCard v-for="(item, idx) in state.items" :key="idx">
              <template #header>
                <div class="flex justify-between items-center gap-2">
                  <UInputMenu
                    v-model="itemSelections[idx] as any"
                    v-model:search-term="itemSearchTerms[idx]"
                    :items="itemOpts[idx]"
                    value-key="item_code"
                    label-key="item_name"
                    description-key="description"
                    :placeholder="itemSelections[idx]?.item_name || 'Search product...'"
                    class="flex-1"
                    :disabled="submitting"
                    @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                    @update:model-value="() => onItemPicked(idx)"
                  />
                  <UButton color="error" variant="ghost" icon="i-lucide-x" size="sm" :disabled="submitting" @click="removeItem(idx)" />
                </div>
              </template>

              <div class="grid grid-cols-3 gap-2">
                <UFormField label="Qty" size="xs">
                  <UInput v-model="item.qty" type="number" :min="1" :step="1" :disabled="submitting" />
                </UFormField>
                <UFormField label="Buy Rate" size="xs">
                  <UInput v-model="item.rate" type="number" :min="0" :step="0.01" :disabled="submitting" />
                </UFormField>
                <UFormField label="Sell Rate" size="xs">
                  <UInput v-model="item.sell_rate" type="number" :min="0" :step="0.01" :disabled="submitting" />
                </UFormField>
              </div>

              <template #footer>
                <div class="text-right text-xs">
                  <span class="text-[var(--ui-text-muted)]">Total Buy: </span>
                  <span class="font-medium">{{ ((item.qty || 0) * (item.rate || 0)).toFixed(2) }}</span>
                </div>
              </template>
            </UCard>
          </div>
        </div>
      </div>

      <!-- Shared footer (rendered on both layouts — inside UForm) -->
      <div class="flex justify-between items-center pt-4 border-t border-[var(--ui-border)]">
        <span class="text-lg font-bold">Total: {{ grandTotal.toFixed(2) }}</span>
        <UButton type="submit" color="primary" :loading="submitting" :disabled="submitting">
          Submit Purchase
        </UButton>
      </div>
    </UForm>
    </div>
```

- [ ] **Step 2: Build and verify no TS errors**

Run: `npm run build` in `frontend/`
Expected: Build succeeds.

---

### Task 3: Verify

- [ ] **Step 1: Build the project**

Run: `npm run build` in `frontend/`
Expected: Clean build, no errors, no warnings.

- [ ] **Step 2: Dev server smoke test**

Run: `npm run dev` in `frontend/`
Manual checks:
- Desktop viewport (>768px): modal opens with 6-column grid (unchanged)
- Mobile viewport (<768px): modal opens fullscreen, items render as UCards, no horizontal scroll
- Both layouts share reactive state — edit a field on mobile, resize to desktop, value persists
- Confirmation screen works identically on both widths
- Submit flow unchanged (creates PO, PR, PI, Cash Payment Entry)
