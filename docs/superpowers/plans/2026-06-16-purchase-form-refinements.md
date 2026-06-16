# Purchase Form Refinements + New Item Creation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorder/resize mobile purchase form fields and add inline new-item creation from the product dropdown.

**Architecture:** Two independent backend scripts (create_item.py, get_item_groups.py) called via ErpNextService, plus a new CreateItemModal.vue component embedded in PurchaseForm.vue. The "+ Create New Item" option is a synthetic entry appended to the product dropdown items.

**Tech Stack:** Vue 3 + Nuxt UI + zod + axios (frontend), Python/frappe (ERPNext server scripts)

---

### Task 1: Mobile Layout Reorder + Sizing

**Files:**
- Modify: `frontend/src/components/PurchaseForm.vue:74-151`

- [ ] **Step 1: Reorder mobile header fields and fix sizing**

In the `md:hidden` block, rearrange the three `UFormField` blocks so Invoice Date comes first, then Supplier, then Invoice No.

Remove `size="xs"` from all three `UFormField` components and from the `USelectMenu` (supplier). Add `class="w-full"` to the supplier `USelectMenu` for full-width consistency.

The mobile block (lines 74-152) currently has this order:
1. Supplier (lines 75-86)
2. Invoice No. (lines 87-89)
3. Invoice Date (lines 90-99)

Change to:
1. Invoice Date → 2. Supplier → 3. Invoice No.

Here is the replacement for lines 74-152:

```vue
      <!-- Mobile: stacked single-column + UCards for items -->
      <div class="md:hidden space-y-4">
        <UFormField label="Invoice Date">
          <UPopover>
            <UButton color="neutral" variant="subtle" icon="i-lucide-calendar" class="w-full justify-start" :disabled="submitting">
              {{ displayDate }}
            </UButton>
            <template #content>
              <UCalendar v-model="state.invoiceDate" class="p-2" />
            </template>
          </UPopover>
        </UFormField>
        <UFormField label="Supplier" name="supplier" required>
          <USelectMenu
            v-model="selectedSupplier as any"
            :items="supplierItems"
            value-key="name"
            label-key="supplier_name"
            placeholder="Select supplier..."
            class="w-full"
            :search-input="{ placeholder: 'Search...' }"
            :disabled="submitting"
          />
        </UFormField>
        <UFormField label="Invoice No.">
          <UInput v-model="state.invoiceNumber" placeholder="Optional" class="w-full" :disabled="submitting" />
        </UFormField>
```

**Changes summary**: Date block moved before Supplier block. Removed `size="xs"` from all three `UFormField`. Removed `size="xs"` from `USelectMenu`. Added `class="w-full"` to `USelectMenu`.

- [ ] **Step 2: Verify mobile layout in item cards uses same sizing**

Remove `size="xs"` from the product `USelectMenu` in the item cards (line 119). Replace:

```vue
                    <USelectMenu
                      v-model="itemSelections[idx] as any"
                      v-model:search-term="itemSearchTerms[idx]"
                      :items="itemOpts[idx]"
                      value-key="item_code"
                      label-key="item_name"
                      description-key="description"
                      :placeholder="itemSelections[idx]?.item_name || 'Search product...'"
                      size="xs"
                      :ignore-filter="true"
                      :search-input="{ placeholder: 'Search products...' }"
                      class="flex-1"
                      :disabled="submitting"
                      @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                      @update:model-value="() => onItemPicked(idx)"
                    />
```

With (removing `size="xs"`):

```vue
                    <USelectMenu
                      v-model="itemSelections[idx] as any"
                      v-model:search-term="itemSearchTerms[idx]"
                      :items="itemOpts[idx]"
                      value-key="item_code"
                      label-key="item_name"
                      description-key="description"
                      :placeholder="itemSelections[idx]?.item_name || 'Search product...'"
                      :ignore-filter="true"
                      :search-input="{ placeholder: 'Search products...' }"
                      class="flex-1"
                      :disabled="submitting"
                      @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                      @update:model-value="() => onItemPicked(idx)"
                    />
```

- [ ] **Step 3: Build and verify no errors**

Run: `npm run build` in `frontend/`

Expected: Build succeeds with no TypeScript errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/PurchaseForm.vue
git commit -m "fix: reorder mobile purchase header fields and increase touch target size"
```

---

### Task 2: Backend — Create Item Script

**Files:**
- Create: `erpnext/server_scripts/create_item.py`

- [ ] **Step 1: Write the create_item.py script**

```python
company = frappe.form_dict.get("company")
item_name = frappe.form_dict.get("item_name", "").strip()
item_group = frappe.form_dict.get("item_group", "").strip()
buying_price = float(frappe.form_dict.get("buying_price", 0) or 0)
selling_price = float(frappe.form_dict.get("selling_price", 0) or 0)

if not company:
    frappe.throw("Company is required")

if not item_name:
    frappe.throw("Item name is required")

if not item_group:
    frappe.throw("Item group is required")

# Check for duplicate item_code
if frappe.db.exists("Item", item_name):
    frappe.throw(f"Item '{item_name}' already exists")

# Create Item doctype
item = frappe.get_doc({
    "doctype": "Item",
    "item_code": item_name,
    "item_name": item_name,
    "item_group": item_group,
    "stock_uom": "Nos",
    "is_stock_item": 1,
    "include_item_in_manufacturing": 0,
})
item.insert(ignore_permissions=True)

# Get price lists
buying_pl = frappe.db.get_value("Buying Settings", None, "buying_price_list") or "Standard Buying"
selling_pl = frappe.db.get_value("Selling Settings", None, "selling_price_list") or "Standard Selling"

# Create buying Item Price if price > 0
if buying_price > 0:
    buy_ip = frappe.get_doc({
        "doctype": "Item Price",
        "item_code": item_name,
        "price_list": buying_pl,
        "buying": 1,
        "price_list_rate": buying_price,
    })
    buy_ip.insert(ignore_permissions=True)

# Create selling Item Price if price > 0
if selling_price > 0:
    sell_ip = frappe.get_doc({
        "doctype": "Item Price",
        "item_code": item_name,
        "price_list": selling_pl,
        "selling": 1,
        "price_list_rate": selling_price,
    })
    sell_ip.insert(ignore_permissions=True)

frappe.response["data"] = {
    "item_code": item_name,
    "item_name": item_name,
    "last_purchase_rate": buying_price,
    "last_selling_rate": selling_price,
    "description": "Buy: {0} | Sell: {1}".format(buying_price, selling_price),
}
```

- [ ] **Step 2: Commit**

```bash
git add erpnext/server_scripts/create_item.py
git commit -m "feat: add create_item server script for ERPNext"
```

---

### Task 3: Backend — Get Item Groups Script

**Files:**
- Create: `erpnext/server_scripts/get_item_groups.py`

- [ ] **Step 1: Write the get_item_groups.py script**

```python
root = frappe.form_dict.get("root", "Products")

if not frappe.db.exists("Item Group", root):
    frappe.response["data"] = []
else:
    groups = frappe.get_all(
        "Item Group",
        filters={"name": ["like", f"{root}%"]},
        fields=["name"],
        order_by="name",
    )
    frappe.response["data"] = [{"name": g.name} for g in groups]
```

- [ ] **Step 2: Commit**

```bash
git add erpnext/server_scripts/get_item_groups.py
git commit -m "feat: add get_item_groups server script for ERPNext"
```

---

### Task 4: Frontend — ErpNextService New Methods

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts:376-383` (add after existing `searchItems`)

- [ ] **Step 1: Add createItem and getItemGroups methods**

Add these two methods to the `ErpNextService` class, right after the `searchItems` method (after line 383):

```typescript
  public createItem(itemName: string, itemGroup: string, buyingPrice: number, sellingPrice: number) {
    const authStore = useAuthStore();
    return this.instance
      .post<{ data?: ItemOption }>("/api/v2/method/create_item", {
        company: authStore.company,
        item_name: itemName,
        item_group: itemGroup,
        buying_price: buyingPrice,
        selling_price: sellingPrice,
      })
      .then((resp) => resp?.data.data);
  }

  public getItemGroups(root?: string) {
    return this.instance
      .get<ErpNextResponse<{ name: string }>>("/api/v2/method/get_item_groups", {
        params: { root: root || "Products" },
      })
      .then((resp) => resp?.data.data || []);
  }
```

- [ ] **Step 2: Build to verify no TypeScript errors**

Run: `npm run build` in `frontend/`

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/services/ErpNextService.ts
git commit -m "feat: add createItem and getItemGroups to ErpNextService"
```

---

### Task 5: Frontend — CreateItemModal Component

**Files:**
- Create: `frontend/src/components/CreateItemModal.vue`

- [ ] **Step 1: Write the CreateItemModal.vue component**

```vue
<template>
  <UModal v-model:open="open" title="Create New Item" :dismissible="false">
    <template #body>
      <div class="p-4">
        <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
          <UFormField label="Item Name / Code" name="itemName" required>
            <UInput
              v-model="state.itemName"
              placeholder="e.g. Stainless Steel Bolt"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <UFormField label="Item Group" name="itemGroup" required>
            <USelectMenu
              v-model="state.itemGroup as any"
              :items="itemGroupOpts"
              value-key="name"
              label-key="name"
              placeholder="Select group..."
              class="w-full"
              :disabled="creating || itemGroupsLoading"
              :loading="itemGroupsLoading"
            />
          </UFormField>
          <UFormField label="Buying Price" name="buyingPrice" required>
            <UInput
              v-model="state.buyingPrice"
              type="number"
              :min="0"
              :step="0.01"
              placeholder="0.00"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <UFormField label="Selling Price" name="sellingPrice" required>
            <UInput
              v-model="state.sellingPrice"
              type="number"
              :min="0"
              :step="0.01"
              placeholder="0.00"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <div class="flex justify-end gap-2 pt-2">
            <UButton color="neutral" variant="outline" :disabled="creating" @click="open = false">
              Cancel
            </UButton>
            <UButton type="submit" color="primary" :loading="creating" :disabled="creating">
              Create Item
            </UButton>
          </div>
        </UForm>
      </div>
    </template>
  </UModal>
</template>

<script setup lang="ts">
import * as z from "zod";
import { reactive, ref, onMounted } from "vue";
import { ErpNextService, type ItemOption } from "@/services/ErpNextService";

const toast = useToast();
const erpnext = new ErpNextService();

const open = defineModel<boolean>("open", { default: false });
const emit = defineEmits<{
  onCreated: [item: ItemOption];
}>();

const creating = ref(false);
const itemGroupsLoading = ref(false);
const itemGroupOpts = ref<{ name: string }[]>([]);

const state = reactive({
  itemName: "",
  itemGroup: "",
  buyingPrice: 0,
  sellingPrice: 0,
});

const schema = z.object({
  itemName: z.string().min(1, "Item name is required"),
  itemGroup: z.string().min(1, "Item group is required"),
  buyingPrice: z.number().gte(0, "Must be >= 0"),
  sellingPrice: z.number().gte(0, "Must be >= 0"),
});

onMounted(async () => {
  itemGroupsLoading.value = true;
  try {
    const groups = await erpnext.getItemGroups("Products");
    if (groups?.length) itemGroupOpts.value = groups;
  } catch {
    /* ignore */
  } finally {
    itemGroupsLoading.value = false;
  }
});

async function onSubmit() {
  creating.value = true;
  try {
    const result = await erpnext.createItem(
      state.itemName,
      state.itemGroup,
      state.buyingPrice,
      state.sellingPrice,
    );
    if (result) {
      toast.add({ title: "Item created", description: `"${result.item_name}" added successfully`, color: "success" });
      emit("onCreated", result);
      open.value = false;
      state.itemName = "";
      state.itemGroup = "";
      state.buyingPrice = 0;
      state.sellingPrice = 0;
    }
  } catch {
    toast.add({ title: "Error", description: "Failed to create item. It may already exist.", color: "error" });
  } finally {
    creating.value = false;
  }
}
</script>
```

- [ ] **Step 2: Build to verify no errors**

Run: `npm run build` in `frontend/`

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/CreateItemModal.vue
git commit -m "feat: add CreateItemModal component for inline item creation"
```

---

### Task 6: Frontend — Integrate CreateItemModal Into PurchaseForm

**Files:**
- Modify: `frontend/src/components/PurchaseForm.vue`

This task has three sub-parts: import, template changes, logic changes.

- [ ] **Step 6a: Import CreateItemModal**

Add the import at line 223, alongside the existing ErpNextService import:

```typescript
import { ErpNextService, type ItemOption, type SupplierOption, type WarehouseOption } from "@/services/ErpNextService";
import CreateItemModal from "@/components/CreateItemModal.vue";
```

- [ ] **Step 6b: Add ref for the modal and the active row index**

Add these new `ref` declarations after line 271 (after `itemTimers`):

```typescript
const createItemModalOpen = ref(false);
const activeCreateItemRow = ref<number>(-1);
```

- [ ] **Step 6c: Add CreateItemModal to the template**

Add the modal component right before the closing `</template>` tag (after line 214, inside the outer `<div>` but after the `v-if`/`v-else` block):

```vue
    <CreateItemModal v-model:open="createItemModalOpen" @on-created="onNewItemCreated" />
```

- [ ] **Step 6d: Add ignore-filter to desktop product UInputMenu**

Add `:ignore-filter="true"` to the desktop product `UInputMenu` (line 46) so the "+ Create New Item" option is never filtered out:

```vue
            <UInputMenu
              v-model="itemSelections[idx] as any"
              v-model:search-term="itemSearchTerms[idx]"
              :items="itemOpts[idx]"
              value-key="item_code"
              label-key="item_name"
              description-key="description"
              :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
              class="w-full"
              :ignore-filter="true"
              :disabled="submitting"
              @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
              @update:model-value="() => onItemPicked(idx)"
            />
```

- [ ] **Step 6e: Append "+ Create New Item" to item search results**

Modify `onItemOpen` (line 345) to append a synthetic option after loading items. Replace lines 345-352:

```typescript
async function onItemOpen(idx: number) {
  if (!itemOpts.value[idx]?.length) {
    try {
      const results = await erpnext.searchItems("");
      if (results) itemOpts.value[idx] = results;
    } catch { /* ignore */ }
  }
  ensureCreateNewOption(idx);
}
```

Add this new helper function after `onItemOpen`:

```typescript
function ensureCreateNewOption(idx: number) {
  const opts = itemOpts.value[idx];
  if (!opts || !Array.isArray(opts)) return;
  const hasCreateNew = opts.some((o) => (o as any).item_code === "__create_new__");
  if (!hasCreateNew) {
    opts.push({
      item_code: "__create_new__",
      item_name: "+ Create New Item",
      last_purchase_rate: 0,
      last_selling_rate: 0,
      description: "",
    } as ItemOption);
  }
}
```

Also modify the `onMounted` handler (line 327) to call `ensureCreateNewOption` after loading initial items. Replace line 327:

```typescript
  // Preload items for first row
  try {
    const items = await erpnext.searchItems("");
    if (items) {
      itemOpts.value[0] = items;
      ensureCreateNewOption(0);
    }
  } catch { /* ignore */ }
```

- [ ] **Step 6f: Intercept "+ Create New Item" selection in onItemPicked**

Modify `onItemPicked` (lines 354-366) to check for the sentinel:

```typescript
function onItemPicked(idx: number) {
  const itemCode = itemSelections.value[idx] as unknown as string;
  if (!itemCode) return;

  if (itemCode === "__create_new__") {
    itemSelections.value[idx] = null;
    activeCreateItemRow.value = idx;
    createItemModalOpen.value = true;
    return;
  }

  const sel = itemOpts.value[idx]?.find((i) => i.item_code === itemCode);
  if (sel) {
    const target = state.items[idx];
    if (!target) return;
    target.item_code = sel.item_code;
    target.item_name = sel.item_name;
    if (sel.last_purchase_rate) target.rate = sel.last_purchase_rate;
    if (sel.last_selling_rate) target.sell_rate = sel.last_selling_rate;
  }
}
```

- [ ] **Step 6g: Add onNewItemCreated handler**

Add this handler after `onItemPicked` (after line 366):

```typescript
function onNewItemCreated(item: ItemOption) {
  const idx = activeCreateItemRow.value;
  if (idx < 0 || idx >= state.items.length) return;
  const target = state.items[idx];
  if (!target) return;
  target.item_code = item.item_code;
  target.item_name = item.item_name;
  if (item.last_purchase_rate) target.rate = item.last_purchase_rate;
  if (item.last_selling_rate) target.sell_rate = item.last_selling_rate;
  itemSelections.value[idx] = item;

  // Refresh item options for this row so the new item appears in search
  erpnext.searchItems("").then((results) => {
    if (results) {
      itemOpts.value[idx] = results;
      ensureCreateNewOption(idx);
    }
  }).catch(() => { /* ignore */ });
}
```

- [ ] **Step 8: Build and verify no errors**

Run: `npm run build` in `frontend/`

Expected: Build succeeds with no TypeScript errors.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/components/PurchaseForm.vue
git commit -m "feat: integrate CreateItemModal into purchase form product dropdown"
```

---
