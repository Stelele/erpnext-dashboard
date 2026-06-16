# Mobile Purchase Form Refinements + New Item Creation — Design Spec

**Date:** 2026-06-16
**Status:** Draft

## Scope

Two improvements to the purchase form:

1. **Mobile layout refinements** — reorder header fields and increase touch target size
2. **New item creation** — allow creating items in ERPNext directly from the purchase form

## Part 1: Mobile Layout Refinements

### Current State

PurchaseForm.vue mobile block (lines 74–152) renders three header fields in this order:
Supplier → Invoice No. → Invoice Date

All three use `size="xs"` on `UFormField` and `USelectMenu`/`UInput`. The `USelectMenu` for supplier has no explicit `class="w-full"` and renders noticeably narrower than the `UInput` for invoice number.

### Changes

1. **Reorder fields** to: Invoice Date → Supplier → Invoice No.
2. **Remove `size="xs"`** from all three `UFormField` components — use default size for taller touch targets.
3. **Add `class="w-full"`** to the `USelectMenu` for supplier (consistency with `UInput` already having it).
4. **Remove `size="xs"`** from the `USelectMenu` itself (both supplier and product selectors).
5. **Match button sizing** on the date picker `UButton` — add `class="w-full"` (it already has `justify-start`).

### Affected Code

`frontend/src/components/PurchaseForm.vue`, mobile block (lines 74–151):
- Reorder the three `UFormField` blocks
- Remove `size="xs"` attributes
- Add `class="w-full"` to supplier `USelectMenu`
- Adjust `UButton` for date picker

No changes to desktop layout, validation, or submit logic.

### Non-Goals

- Desktop layout unchanged
- Item cards in mobile view unchanged
- No backend changes

---

## Part 2: New Item Creation from Purchase Form

### Flow

```
User opens product dropdown (USelectMenu)
  └─ Sees existing items + "+ Create New Item" at bottom
       └─ Clicks "+ Create New Item"
            └─ CreateItemModal opens (UModal)
                 ├─ Item Name (text input, doubles as item_code)
                 ├─ Item Group (USelectMenu — Products + sub-groups)
                 ├─ Buying Price (number input)
                 ├─ Selling Price (number input)
                 └─ [Cancel] [Create Item]
                      └─ On submit: calls create_item API
                           ├─ Creates Item doctype
                           ├─ Creates Buying Item Price
                           ├─ Creates Selling Item Price
                           └─ Returns new item
                                └─ Auto-fills current purchase form row
```

### Backend

#### New Script: `erpnext/server_scripts/create_item.py`

**Endpoint:** `POST /api/v2/method/create_item`

**Parameters:**
| Param | Type | Required | Description |
|-------|------|----------|-------------|
| item_name | string | yes | Item name, also used as item_code |
| item_group | string | yes | Item group name (e.g. "Products") |
| buying_price | float | yes | Price in the buying price list |
| selling_price | float | yes | Price in the selling price list |
| company | string | yes | ERPNext company |

**Logic:**
1. Create `Item` doctype with `item_code = item_name`, `item_group`, `item_name`
2. Find buying price list from `Buying Settings` (same pattern as `search_items.py`)
3. Find selling price list from `Selling Settings`
4. Create `Item Price` for buying (if `buying_price > 0`)
5. Create `Item Price` for selling (if `selling_price > 0`)
6. Return `{ item_code, item_name, last_purchase_rate, last_selling_rate }`

#### New Script: `erpnext/server_scripts/get_item_groups.py`

**Endpoint:** `GET /api/v2/method/get_item_groups`

**Query params:**
| Param | Type | Required | Description |
|-------|------|----------|-------------|
| root | string | no | Root item group to fetch children of (default: "Products") |

**Logic:**
1. Query `Item Group` doctype for the root group and all its descendants
2. Return list of `{ name }` objects

### Frontend

#### New Component: `frontend/src/components/CreateItemModal.vue`

A standalone modal component that:
- Accepts props: `company` (string)
- Emits: `onCreated` with `ItemOption` (same shape as search results)
- Contains a `UForm` with `zod` validation (same pattern as `PurchaseForm.vue`):
  - Item name: required non-empty string
  - Item group: required non-empty string
  - Buying price: number ≥ 0
  - Selling price: number ≥ 0
- On mount: fetches item groups via `ErpNextService.getItemGroups()`
- On submit: calls `ErpNextService.createItem()` then emits result

#### New Service Methods: `ErpNextService`

```typescript
createItem(itemName: string, itemGroup: string, buyingPrice: number, sellingPrice: number): Promise<ItemOption>

getItemGroups(root?: string): Promise<{ name: string }[]>
```

#### Changes to `PurchaseForm.vue`

1. **"+ Create New Item" option in product dropdown**: Append a synthetic option to `itemOpts[idx]` at each row. The option has a special `item_code` value (e.g. `__create_new__`). When selected, open the `CreateItemModal` instead of treating it as a normal item.

2. **Embed `CreateItemModal`**: Add `<CreateItemModal ref="createItemModal" :company="..." @on-created="onNewItemCreated(idx)" />` in the template.

3. **`onNewItemCreated(idx, item)` handler**: Replace the current row's product with the newly created item — same logic as `onItemPicked` but using the returned item directly.

4. **Detect the "+ Create New Item" selection**: In the product `USelectMenu`'s `@update:model-value` handler, check if the selected value is the special sentinel. If so, intercept and open the modal.

### Component Tree

```
StockView.vue
  └─ UModal
       └─ PurchaseForm.vue
            ├─ (existing form fields)
            ├─ USelectMenu (product) → "+ Create New Item" option
            └─ CreateItemModal.vue (conditionally rendered, v-model:open)
```

### Nuxt UI Components Used (new)

| Component | Usage |
|-----------|-------|
| `UModal` | CreateItemModal wrapper |
| `UForm` + `UFormField` | Form fields |
| `UInput` | Item name, prices |
| `USelectMenu` | Item group dropdown |
| `UButton` | Cancel / Create actions |
| Toast (`useToast`) | Success/error feedback |

### Error Handling

- Duplicate item name: ERPNext will return an error, shown as toast
- Network failure: toast with error message, modal stays open
- Validation: `UForm` prevents submit with invalid data

### Non-Goals

- No item image upload
- No item description or other optional fields
- No batch item creation
- No editing of existing items

---

## Files Changed

| File | Change |
|------|--------|
| `frontend/src/components/PurchaseForm.vue` | Mobile layout reorder + sizing; "+ Create New Item" trigger; embed CreateItemModal |
| `frontend/src/components/CreateItemModal.vue` | **New** — item creation modal |
| `frontend/src/services/ErpNextService.ts` | **New methods** — `createItem()`, `getItemGroups()` |
| `erpnext/server_scripts/create_item.py` | **New** — ERPNext item creation script |
| `erpnext/server_scripts/get_item_groups.py` | **New** — ERPNext item group listing script |

## Testing

- Manual: create a new item from mobile view, verify it appears in purchase form
- Manual: create a new item from desktop view, verify same flow
- Manual: verify item is searchable immediately after creation
- Manual: verify duplicate item name shows error toast
- Manual: verify mobile fields are reordered and taller
