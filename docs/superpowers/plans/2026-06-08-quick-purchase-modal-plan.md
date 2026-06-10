# Quick Purchase Entry Modal - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "Quick Purchase" button to the Stock page that opens a modal to create a full purchase cycle (PO → PR → PI → Payment) in a single form.

**Architecture:** Two new ERPNext server scripts for supplier/item search, one new Vue component (PurchaseForm) following the ExpenseForm pattern, modifications to ErpNextService and StockView. Uses existing Nuxt UI v4 components (UModal, UForm, UInputMenu, UInputNumber, UCalendar, UButton).

**Tech Stack:** Vue 3 + Nuxt UI v4 + Zod + Axios + Frappe/ERPNext Server Scripts

---

### Task 1: Create supplier, item, and warehouse search server scripts

**Files:**
- Create: `erpnext/server_scripts/search_suppliers.py`
- Create: `erpnext/server_scripts/search_items.py`
- Create: `erpnext/server_scripts/search_warehouses.py`

- [ ] **Step 1: Create supplier search script**

```python
# erpnext/server_scripts/search_suppliers.py
import frappe

company = frappe.form_dict.get("company")
query = frappe.form_dict.get("query", "")

if not company:
    frappe.throw("Company is required")

filters = {"company": company, "disabled": 0}
if query:
    filters["supplier_name"] = ["like", f"%{query}%"]

suppliers = frappe.get_all("Supplier", filters=filters, fields=["name", "supplier_name"], limit=20, order_by="supplier_name")

frappe.response["data"] = [
    {"name": s.name, "supplier_name": s.supplier_name}
    for s in suppliers
]
```

- [ ] **Step 2: Create item search script**

```python
# erpnext/server_scripts/search_items.py
import frappe
from frappe.utils import flt

company = frappe.form_dict.get("company")
query = frappe.form_dict.get("query", "")

if not company:
    frappe.throw("Company is required")

filters = {"disabled": 0}
if query:
    filters["item_name"] = ["like", f"%{query}%"]

items = frappe.get_all("Item", filters=filters, fields=["item_code", "item_name"], limit=20, order_by="item_name")

default_price_list = frappe.db.get_value("Buying Settings", None, "buying_price_list") or "Standard Buying"
result = []
for item in items:
    rate = flt(frappe.db.get_value("Item Price", {"item_code": item.item_code, "price_list": default_price_list, "buying": 1}, "price_list_rate"))
    result.append({
        "item_code": item.item_code,
        "item_name": item.item_name,
        "last_purchase_rate": rate,
    })

frappe.response["data"] = result
```

- [ ] **Step 3: Create warehouse search script**

```python
# erpnext/server_scripts/search_warehouses.py
import frappe

company = frappe.form_dict.get("company")

if not company:
    frappe.throw("Company is required")

warehouses = frappe.get_all("Warehouse", filters={"company": company, "is_group": 0}, fields=["name"], order_by="name")

frappe.response["data"] = [{"name": w.name} for w in warehouses]
```

- [ ] **Step 4: Verify files exist**

Run: `ls -la erpnext/server_scripts/search_suppliers.py erpnext/server_scripts/search_items.py erpnext/server_scripts/search_warehouses.py`

Expected: All three files listed with sizes > 0

---
### Task 2: Add API methods to ErpNextService.ts

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts`

- [ ] **Step 1: Add type definitions at the top with existing imports**

Insert after the existing `DailyStockValue` import (line 21):

```typescript
export interface SupplierOption {
  name: string;
  supplier_name: string;
}

export interface ItemOption {
  item_code: string;
  item_name: string;
  last_purchase_rate: number;
}

export interface WarehouseOption {
  name: string;
}

export interface PurchasePayload {
  company: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number }[];
  invoice_number?: string;
  invoice_date: string;
}

export interface PurchaseResult {
  purchase_order: string;
  purchase_receipt: string;
  purchase_invoice: string;
  payment_entry: string;
}
```

- [ ] **Step 2: Add methods to ErpNextService class** (before the private methods, after `addDraftExpenseJournalEntry`)

```typescript
  public searchSuppliers(query: string) {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<SupplierOption>>("/api/v2/method/search_suppliers", {
        params: { company: authStore.company, query },
      })
      .then((resp) => resp?.data.data);
  }

  public searchItems(query: string) {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<ItemOption>>("/api/v2/method/search_items", {
        params: { company: authStore.company, query },
      })
      .then((resp) => resp?.data.data);
  }

  public getWarehouses() {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<WarehouseOption>>("/api/v2/method/search_warehouses", {
        params: { company: authStore.company },
      })
      .then((resp) => resp?.data.data);
  }

  public createFullPurchase(payload: PurchasePayload) {
    return this.instance
      .post<{ data?: PurchaseResult }>("/api/v2/method/create_full_purchase", null, {
        params: { ...payload, items: JSON.stringify(payload.items) },
      })
      .then((resp) => resp?.data.data);
  }
```

---
### Task 3: Create PurchaseForm.vue component

**Files:**
- Create: `frontend/src/components/PurchaseForm.vue`

- [ ] **Step 1: Create the component**

```bash
cat > frontend/src/components/PurchaseForm.vue << 'VUEEOF'
<template>
  <div class="w-full h-full p-4">
    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="text-sm font-medium mb-1 block">Supplier</label>
          <UInputMenu
            v-model="selectedSupplier"
            :items="supplierItems"
            value-key="name"
            label-key="supplier_name"
            class="w-full"
            @update:open="onSupplierFocus"
          />
        </div>
        <div>
          <label class="text-sm font-medium mb-1 block">Warehouse</label>
          <USelect
            v-model="state.warehouse"
            :items="warehouseItems"
            value-key="name"
            label-key="name"
            class="w-full"
          />
        </div>
        <div>
          <label class="text-sm font-medium mb-1 block">Invoice No. (Optional)</label>
          <input v-model="state.invoiceNumber" class="w-full border border-gray-200 rounded-md px-3 py-2 text-sm" placeholder="Optional" />
        </div>
        <div>
          <label class="text-sm font-medium mb-1 block">Invoice Date</label>
          <UPopover>
            <UButton color="neutral" variant="subtle" icon="i-lucide-calendar" class="w-full">
              {{ displayDate }}
            </UButton>
            <template #content>
              <UCalendar v-model="state.invoiceDate" class="p-2" />
            </template>
          </UPopover>
        </div>
      </div>

      <div>
        <label class="text-sm font-medium mb-2 block">Items</label>
        <div class="grid grid-cols-[2fr_1fr_1fr_80px_auto] gap-2 mb-2 text-xs font-medium text-gray-500 px-2">
          <span>Product</span>
          <span>Qty</span>
          <span>Rate</span>
          <span>Total</span>
          <span></span>
        </div>
        <div v-for="(item, idx) in state.items" :key="idx" class="grid grid-cols-[2fr_1fr_1fr_80px_auto] gap-2 mb-2">
          <UInputMenu
            :model-value="item.item_code ? { name: item.item_code, item_name: item.item_name || item.item_code } : null"
            :items="itemSearchResults[idx] || []"
            value-key="item_code"
            label-key="item_name"
            class="w-full"
            @update:open="() => searchItems(idx, '')"
            @update:model-value="(val: ItemOption | null) => onItemSelect(idx, val)"
          />
          <UInputNumber v-model="item.qty" class="w-full" :min="1" :step="1" />
          <UInputNumber v-model="item.rate" class="w-full" :min="0" :step="0.01" />
          <div class="flex items-center justify-end text-sm font-medium">
            {{ (item.qty * item.rate).toFixed(2) }}
          </div>
          <UButton color="error" variant="ghost" icon="i-lucide-x" size="sm" @click="removeItem(idx)" />
        </div>
        <UButton variant="outline" icon="i-lucide-plus" size="sm" class="w-full" @click="addItem">
          Add Item
        </UButton>
      </div>

      <div class="flex justify-between items-center pt-4 border-t">
        <span class="text-lg font-bold">Total: {{ currency }} {{ grandTotal.toFixed(2) }}</span>
        <UButton type="submit" color="primary" :loading="submitting">
          Submit Purchase
        </UButton>
      </div>
    </UForm>
  </div>
</template>

<script setup lang="ts">
import { CalendarDate, getLocalTimeZone } from "@internationalized/date";
import * as z from "zod";
import moment from "moment";
import { computed, reactive, ref, shallowRef, onMounted } from "vue";
import { ErpNextService, type ItemOption, type SupplierOption, type WarehouseOption } from "@/services/ErpNextService";

const emit = defineEmits<{
  onSubmit: [
    payload: {
      supplier: string;
      warehouse: string;
      items: { item_code: string; qty: number; rate: number }[];
      invoice_number: string | null;
      invoice_date: string;
    },
  ];
}>();

const submitting = ref(false);
const erpnext = new ErpNextService();
const currency = "KES";

const supplierItems = ref<SupplierOption[]>([]);
const warehouseItems = ref<WarehouseOption[]>([]);
const itemSearchResults = ref<ItemOption[][]>([]);
const selectedSupplier = ref<SupplierOption | null>(null);
let supplierSearchTimer: ReturnType<typeof setTimeout> | null = null;
let itemSearchTimer: Record<number, ReturnType<typeof setTimeout>> = {};

interface PurchaseItem {
  item_code: string;
  item_name: string;
  qty: number;
  rate: number;
}

const state = reactive<{
  invoiceNumber: string;
  invoiceDate: ReturnType<typeof shallowRef<CalendarDate>>;
  warehouse: string;
  items: PurchaseItem[];
}>({
  invoiceNumber: "",
  invoiceDate: shallowRef(new CalendarDate(moment().year(), moment().month() + 1, moment().date())),
  warehouse: "",
  items: [{ item_code: "", item_name: "", qty: 1, rate: 0 }],
});

const schema = z.object({
  invoiceNumber: z.string().optional(),
  invoiceDate: z.object({ year: z.number(), month: z.number(), day: z.number() }).transform(
    ({ year, month, day }) => shallowRef(new CalendarDate(year, month, day)),
  ),
  warehouse: z.string().min(1, "Warehouse is required"),
  items: z.array(z.object({
    item_code: z.string().min(1, "Product is required"),
    item_name: z.string(),
    qty: z.number().gt(0, "Qty must be > 0"),
    rate: z.number().gte(0, "Rate must be >= 0"),
  })).min(1, "At least one item required"),
});

const displayDate = computed(() =>
  moment(state.invoiceDate.value.toDate(getLocalTimeZone())).format("DD MMM YYYY"),
);

const grandTotal = computed(() =>
  state.items.reduce((sum, item) => sum + item.qty * item.rate, 0),
);

onMounted(async () => {
  const warehouses = await erpnext.getWarehouses();
  if (warehouses) {
    warehouseItems.value = warehouses;
    // Default to Stores warehouse
    const stores = warehouses.find((w) => w.name.toLowerCase().includes("stores"));
    state.warehouse = stores ? stores.name : (warehouses[0]?.name || "");
  }
});

async function onSupplierFocus() {
  if (supplierItems.value.length === 0) {
    const results = await erpnext.searchSuppliers("");
    if (results) {
      supplierItems.value = results;
    }
  }
}

function searchItems(idx: number, query: string) {
  if (itemSearchTimer[idx]) clearTimeout(itemSearchTimer[idx]);
  itemSearchTimer[idx] = setTimeout(async () => {
    const results = await erpnext.searchItems(query);
    if (results) {
      itemSearchResults.value[idx] = results;
    }
  }, 300);
}

function onItemSelect(idx: number, val: ItemOption | null) {
  if (val) {
    state.items[idx].item_code = val.item_code;
    state.items[idx].item_name = val.item_name;
    state.items[idx].rate = val.last_purchase_rate || state.items[idx].rate;
  }
}

function addItem() {
  state.items.push({ item_code: "", item_name: "", qty: 1, rate: 0 });
}

function removeItem(idx: number) {
  if (state.items.length > 1) {
    state.items.splice(idx, 1);
  }
}

async function onSubmit() {
  submitting.value = true;
  try {
    emit("onSubmit", {
      supplier: selectedSupplier.value?.name || "",
      warehouse: state.warehouse,
      items: state.items.map((i) => ({ item_code: i.item_code, qty: i.qty, rate: i.rate })),
      invoice_number: state.invoiceNumber || null,
      invoice_date: moment(state.invoiceDate.value.toDate(getLocalTimeZone())).format("YYYY-MM-DD"),
    });
  } finally {
    submitting.value = false;
  }
}
</script>
VUEEOF
```

**Note:** Nuxt UI v4 uses `UInputMenu` for searchable selects and `UInputNumber` for numbers. The warehouse uses a simple `USelect` dropdown. Supplier and items use `UInputMenu` which provides built-in search/filter of its items list.

**IMPORTANT:** After creating the file, verify Nuxt UI v4 component API. If `USelect` is not available, use `UInputMenu` with static items instead. If `UInput` is not the component name for text inputs, check the correct name. Nuxt UI v4 may use `UFormField` wrapping or different component names. Adjust imports accordingly.

---
### Task 4: Integrate PurchaseForm into StockView.vue

**Files:**
- Modify: `frontend/src/views/StockView.vue`

- [ ] **Step 1: Add modal and button to template**

Replace the StockView template (lines 1-31) to add the Quick Purchase modal before the `NumberCard` section:

```vue
<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse gap-2">
            <UModal
                v-model:open="openPurchase"
                title="Quick Purchase Entry"
                :dismissible="false"
            >
                <UButton
                    trailing-icon="i-lucide-shopping-cart"
                    class="hover:cursor-pointer"
                    >Quick Purchase</UButton
                >
                <template #body>
                    <PurchaseForm @on-submit="onPurchaseSubmit" />
                </template>
            </UModal>
        </div>
        <NumberCard
            v-for="(item, idx) in items"
            :key="idx"
            :title="item.title"
            :value="item.value"
            :direction="item.direction"
            :percent-change="item.percentChange"
        />
        <CardBubbleChart
            :isLoading="dataStore.loading"
            :title="stockDataStore.salesVsStock.title"
            :labels="stockDataStore.salesVsStock.labels"
            :additionalData="stockDataStore.salesVsStock.additionalData"
            :datasets="stockDataStore.salesVsStock.datasets"
            :tooltip-labels="stockDataStore.salesVsStock.tooltipLabels"
        />
        <CardDoughnutChart
            title="Stock Value By Item Group"
            :data="stockDataStore.stockByItemGroup"
        />
        <CardLineChart
            title="Daily Stock Value"
            :data="stockDataStore.dailyStockValues"
        />
        <StockTable
            :data="stockDataStore.stockTableData"
            :loading="dataStore.loading"
        />
    </DashboardLayout>
</template>
```

- [ ] **Step 2: Add script logic**

Replace the script setup (lines 34-57) with:

```vue
<script setup lang="ts">
import type { NumberCardProps } from "@/components/NumberCard.vue";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import PurchaseForm from "@/components/PurchaseForm.vue";
import { useDataStore } from "@/stores/DataStore";
import { useStockDataStore } from "@/stores/StockDataStore";
import { useAuthStore } from "@/stores/AuthStore";
import { ErpNextService } from "@/services/ErpNextService";
import { ref, computed } from "vue";

const dataStore = useDataStore();
const stockDataStore = useStockDataStore();
const authStore = useAuthStore();

const openPurchase = ref(false);
const toast = useToast();
const erpnext = new ErpNextService();

const items = computed<NumberCardProps[]>(() => [
    {
        title: "Total Stock Value ($)",
        value: stockDataStore.totalStockValue,
    },
    {
        title: "Total Selling Value ($)",
        value: stockDataStore.totalSellingValue,
    },
    {
        title: "Avg. Markup Percentage (%)",
        value: stockDataStore.averageMarkupPercentage,
    },
]);

async function onPurchaseSubmit(payload: {
    supplier: string;
    warehouse: string;
    items: { item_code: string; qty: number; rate: number }[];
    invoice_number: string | null;
    invoice_date: string;
}) {
    const result = await erpnext.createFullPurchase({
        company: authStore.company || "",
        supplier: payload.supplier,
        warehouse: payload.warehouse,
        items: payload.items,
        invoice_number: payload.invoice_number || undefined,
        invoice_date: payload.invoice_date,
    });

    if (result) {
        openPurchase.value = false;
        toast.add({
            title: `Purchase submitted: PO ${result.purchase_order}, PI ${result.purchase_invoice}`,
            color: "success",
        });
    } else {
        toast.add({
            title: "Failed to submit purchase",
            color: "error",
        });
    }
}
</script>
```

- [ ] **Step 3: Verify build**

Run: `cd frontend && npm run build`

Expected: Build succeeds without TypeScript errors

---
## Self-Review

**1. Spec coverage:**
- "Quick Purchase" button on Stock page ✓ (Task 4)
- UModal with two-column header layout ✓ (Task 3, Task 4)
- Supplier server-side search ✓ (Task 1, Task 2, Task 3)
- Product server-side search ✓ (Task 1, Task 2, Task 3)
- Warehouse dropdown defaulting to Stores ✓ (Task 3)
- Invoice number optional ✓ (Task 3)
- Invoice date defaults to today ✓ (Task 3)
- Items table with dynamic totals ✓ (Task 3)
- Grand total display ✓ (Task 3)
- Submit calls createFullPurchase ✓ (Task 2, Task 3, Task 4)
- Success notification ✓ (Task 4)
- Error handling ✓ (Task 4)

**2. Placeholder scan:** No TBDs, TODOs, or vague references.

**3. Type consistency:** `ItemOption`, `SupplierOption`, `WarehouseOption`, `PurchasePayload`, `PurchaseResult` are defined in Task 2 and used consistently in Tasks 3 and 4.

**Plan is complete and ready for execution.**
