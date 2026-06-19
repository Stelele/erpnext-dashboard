# Supplier Creation Modal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add in-place supplier creation (modal) to the PurchaseForm, mirroring the existing item creation pattern.

**Architecture:** Three files changed — add `createSupplier()` API method to ErpNextService.ts, create a new `CreateSupplierModal.vue` component, and integrate it into `PurchaseForm.vue` with a "+ Create New Supplier" option in the supplier dropdown. No backend changes needed — uses ERPNext's built-in REST API (`POST /api/resource/Supplier`).

**Tech Stack:** Vue 3 (script setup), TypeScript, Zod validation, Nuxt UI (UModal, UInput, UButton, UForm), Axios

---

### Task 1: Add `createSupplier()` to ErpNextService.ts

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts` (after `searchSuppliers`, before `searchItems`)

- [ ] **Step 1: Add `createSupplier` method**

Insert after `searchSuppliers` (line 366-373) and before `searchItems` (line 375):

```typescript
  public createSupplier(supplierName: string) {
    return this.instance
      .post<{ data?: SupplierOption }>("/api/resource/Supplier", {
        supplier_name: supplierName,
      })
      .then((resp) => resp?.data.data);
  }
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/services/ErpNextService.ts
git commit -m "feat: add createSupplier API method"
```

---

### Task 2: Create CreateSupplierModal.vue

**Files:**
- Create: `frontend/src/components/CreateSupplierModal.vue`

- [ ] **Step 1: Create the component**

```vue
<template>
  <UModal v-model:open="open" title="Create New Supplier" :dismissible="false">
    <template #body>
      <div class="p-4">
        <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
          <UFormField label="Supplier Name" name="supplierName" required>
            <UInput
              v-model="state.supplierName"
              placeholder="e.g. ABC Supplies Ltd"
              class="w-full"
              :disabled="creating"
            />
          </UFormField>
          <div class="flex justify-end gap-2 pt-2">
            <UButton color="neutral" variant="outline" :disabled="creating" @click="open = false">
              Cancel
            </UButton>
            <UButton type="submit" color="primary" :loading="creating" :disabled="creating">
              Create Supplier
            </UButton>
          </div>
        </UForm>
      </div>
    </template>
  </UModal>
</template>

<script setup lang="ts">
import * as z from "zod";
import { reactive, ref, watch } from "vue";
import { ErpNextService, type SupplierOption } from "@/services/ErpNextService";

const toast = useToast();
const erpnext = new ErpNextService();

const open = defineModel<boolean>("open", { default: false });
const emit = defineEmits<{
  onCreated: [item: SupplierOption];
}>();

const creating = ref(false);

const state = reactive({
  supplierName: "",
});

const schema = z.object({
  supplierName: z.string().min(1, "Supplier name is required"),
});

watch(open, (isOpen) => {
  if (isOpen) {
    state.supplierName = "";
  }
});

async function onSubmit() {
  creating.value = true;
  try {
    const result = await erpnext.createSupplier(state.supplierName);
    if (result) {
      toast.add({ title: "Supplier created", description: `"${result.supplier_name}" added successfully`, color: "success" });
      emit("onCreated", result);
      open.value = false;
      state.supplierName = "";
    }
  } catch {
    toast.add({ title: "Error", description: "Failed to create supplier. It may already exist.", color: "error" });
  } finally {
    creating.value = false;
  }
}
</script>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/components/CreateSupplierModal.vue
git commit -m "feat: add CreateSupplierModal component"
```

---

### Task 3: Integrate supplier modal into PurchaseForm.vue

**Files:**
- Modify: `frontend/src/components/PurchaseForm.vue`

Three changes: (A) import and mount the modal component, (B) add `ensureCreateNewSupplierOption`, (C) handle `__create_new__` selection and `onNewSupplierCreated` callback.

- [ ] **Step 1: Import CreateSupplierModal and add it to template**

At line 226, add import:
```typescript
import CreateSupplierModal from "@/components/CreateSupplierModal.vue";
```

At line 217, after the existing `</template>` closing of `CreateItemModal`, add:
```vue
    <CreateSupplierModal v-model:open="createSupplierModalOpen" @on-created="onNewSupplierCreated" />
```

- [ ] **Step 2: Add supplier modal refs**

After line 279 (`const activeCreateItemRow = ref<number>(-1);`), add:
```typescript
const createSupplierModalOpen = ref(false);
```

- [ ] **Step 3: Add `ensureCreateNewSupplierOption` function**

After `ensureCreateNewOption` (line 363-376), add:
```typescript
function ensureCreateNewSupplierOption() {
  const hasCreateNew = supplierItems.value.some((s) => (s as any).name === "__create_new__");
  if (!hasCreateNew) {
    supplierItems.value.push({
      name: "__create_new__",
      supplier_name: "+ Create New Supplier",
    } as SupplierOption);
  }
}
```

- [ ] **Step 4: Add `onNewSupplierCreated` function**

After `onNewItemCreated` (line 400-418), add:
```typescript
function onNewSupplierCreated(supplier: SupplierOption) {
  selectedSupplier.value = supplier;
  erpnext.searchSuppliers("").then((results) => {
    if (results) {
      supplierItems.value = results;
      ensureCreateNewSupplierOption();
    }
  }).catch(() => { /* ignore */ });
}
```

- [ ] **Step 5: Add `onSupplierPicked` handler, `@update:open` + `@update:model-value` to supplier dropdowns**

In the `onMounted` block (line 309-333), after line 323 (`if (suppliers) supplierItems.value = suppliers;`), add:
```typescript
    ensureCreateNewSupplierOption();
```

On the desktop `UInputMenu` (line 20), change:
```html
            <UInputMenu
              v-model="selectedSupplier as any"
              :items="supplierItems"
              value-key="name"
              label-key="supplier_name"
              placeholder="Search supplier..."
              class="w-full"
              :disabled="submitting"
            />
```
To:
```html
            <UInputMenu
              v-model="selectedSupplier as any"
              :items="supplierItems"
              value-key="name"
              label-key="supplier_name"
              placeholder="Search supplier..."
              class="w-full"
              :disabled="submitting"
              @update:open="(open: boolean) => { if (open) ensureCreateNewSupplierOption() }"
              @update:model-value="onSupplierPicked"
            />
```

On the mobile `USelectMenu` (line 88), change:
```html
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
```
To:
```html
          <USelectMenu
            v-model="selectedSupplier as any"
            :items="supplierItems"
            value-key="name"
            label-key="supplier_name"
            placeholder="Select supplier..."
            class="w-full"
            :search-input="{ placeholder: 'Search...' }"
            :disabled="submitting"
            @update:open="(open: boolean) => { if (open) ensureCreateNewSupplierOption() }"
            @update:model-value="onSupplierPicked"
          />
```

After `onItemPicked` (line 378-398), add the `onSupplierPicked` function:
```typescript
function onSupplierPicked() {
  if ((selectedSupplier.value as any) === "__create_new__") {
    selectedSupplier.value = null;
    createSupplierModalOpen.value = true;
  }
}
```

- [ ] **Step 6: Build and verify**

```bash
cd frontend && npm run build
```

Expected: Build succeeds with no errors.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/components/PurchaseForm.vue
git commit -m "feat: integrate CreateSupplierModal into PurchaseForm"
```
