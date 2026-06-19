# Supplier Creation Modal Design

**Date:** 2026-06-19

## Goal

Add in-place supplier creation to the PurchaseForm, matching the existing item creation pattern (CreateItemModal.vue).

## Design

Mirror the item creation pattern exactly:

### 1. `ErpNextService.ts` — `createSupplier(name)` method

- `POST /api/resource/Supplier` with body `{ supplier_name: name }`
- Returns `SupplierOption` (`{ name, supplier_name }`)
- No custom backend script needed — ERPNext's built-in REST API handles Supplier doctype creation

### 2. `CreateSupplierModal.vue` — New component

- Single-field modal: Supplier Name (required)
- Zod validation (`z.string().min(1)`)
- Opens/closes via `v-model:open`
- Emits `onCreated` with `SupplierOption`
- Same structure as `CreateItemModal.vue` but simpler (no groups, no prices)

### 3. `PurchaseForm.vue` — Supplier dropdown integration

- `ensureCreateNewSupplierOption()` — appends `{ name: "__create_new__", supplier_name: "+ Create New Supplier" }` to `supplierItems`
- Call it in `onMounted` and when supplier dropdown opens
- When `__create_new__` is picked: clear selection, open `CreateSupplierModal`
- `onNewSupplierCreated(supplier)` — sets `selectedSupplier`, refreshes `supplierItems` via `searchSuppliers("")`
- Import and mount `<CreateSupplierModal>` in template

## Files Changed

| File | Change |
|------|--------|
| `frontend/src/services/ErpNextService.ts` | Add `createSupplier()` method |
| `frontend/src/components/CreateSupplierModal.vue` | New file |
| `frontend/src/components/PurchaseForm.vue` | 3 additions to supplier dropdown |

## No Backend Changes

ERPNext's native REST API handles `POST /api/resource/Supplier`. No Python server scripts needed.
