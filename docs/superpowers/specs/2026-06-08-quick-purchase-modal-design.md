# Quick Purchase Entry Modal - Design

## Purpose
Add a "Quick Purchase" button to the Stock page that opens a modal form allowing users to enter a complete purchase cycle (Purchase Order → Purchase Receipt → Purchase Invoice → Payment Entry) in a single form. Patterned after the existing ExpenseForm/UModal pattern in ExpensesView.

## Architecture

### New Files
- `frontend/src/components/PurchaseForm.vue` — Main form component with supplier search, items table, and validation
- `erpnext/server_scripts/search_suppliers.py` — Server script to search suppliers via API
- `erpnext/server_scripts/search_items.py` — Server script to search items via API

### Modified Files
- `frontend/src/views/StockView.vue` — Add "Quick Purchase" button and UModal wrapper
- `frontend/src/services/ErpNextService.ts` — Add `searchSuppliers()`, `searchItems()`, `getWarehouses()`, `createFullPurchase()` methods
- `erpnext/server_scripts/create_full_purchase.py` — Already updated: `invoice_number` optional, `invoice_date` defaults to today

## Component Structure

```
StockView.vue
├── UModal (v-model:open, title="Quick Purchase Entry", :dismissible="false")
│   ├── trigger: UButton "Quick Purchase"
│   └── body: PurchaseForm
│       ├── Two-column header grid
│       │   ├── Supplier: search input (server-side, debounced)
│       │   ├── Warehouse: select dropdown (fetched once, defaults to Stores)
│       │   ├── Invoice Number: text input (optional)
│       │   └── Invoice Date: date picker (defaults to today)
│       ├── Items table
│       │   ├── Header row: Product | Qty | Rate | Total | Actions
│       │   └── Rows: search input | number input | number input | computed display | remove button
│       ├── "+ Add Item" button
│       └── Footer: grand total + "Submit Purchase" button
```

## Data Flow

1. User clicks "Quick Purchase" → modal opens, warehouse list loads, defaults to Stores warehouse
2. User types supplier → debounced API call to `search_suppliers` → results show as dropdown
3. User adds items → each item row has a debounced `search_items` for product selection
4. As user types qty/rate → row total updates reactively, grand total updates
5. User clicks "Submit Purchase" → validation runs → calls `createFullPurchase` API
6. Success → shows created document names, resets form, closes modal
7. Failure → shows error from ERPNext

## API Endpoints

### New Server Scripts
- `search_suppliers` — GET, params: `company`, `query` → returns `[{name, supplier_name}]`
- `search_items` — GET, params: `company`, `query` → returns `[{item_code, item_name, last_purchase_rate}]`

### Existing (Modified)
- `create_full_purchase` — POST, `invoice_number` now optional, `invoice_date` defaults to today

## Validation
- Supplier: required, must exist
- Warehouse: required, must exist
- Items: at least one required, each must have item_code, qty > 0, rate >= 0
- Invoice Date: must be valid date if provided, defaults to today

## Success/Error Handling
- Success: Display created document names (PO, PR, PI, PE), reset form, close modal
- Error: Show error message in form, keep modal open for correction
