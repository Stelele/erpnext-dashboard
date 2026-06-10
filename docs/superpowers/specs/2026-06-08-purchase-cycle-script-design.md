# Purchase Cycle Simplification Script - Design

## Purpose
Simplify ERPNext purchase entry for single-user/small business users by creating a single API call that generates the full purchase cycle: Purchase Order → Purchase Receipt → Purchase Invoice → Payment Entry.

## Architecture
Single server script at `erpnext/server_scripts/create_full_purchase.py` with internal helper functions.

## API Contract

### Input Parameters
- `company` (required) - Company name
- `supplier` (required) - Supplier name
- `warehouse` (required) - Receiving warehouse
- `items` (required) - JSON array of `{item_code, qty, rate}`
- `invoice_number` (required) - Supplier's invoice number
- `invoice_date` (required) - Supplier's invoice date (YYYY-MM-DD)

### Response
Returns created document names: `{purchase_order, purchase_receipt, purchase_invoice, payment_entry}`

## Internal Functions
1. `validate_inputs()` - Check required params, validate supplier/item/warehouse exist
2. `create_purchase_order()` - Create and submit PO
3. `create_purchase_receipt()` - Create and submit PR from PO
4. `create_purchase_invoice()` - Create and submit PI from PR with user's invoice details
5. `create_payment_entry()` - Create and submit cash payment against PI

## Error Handling
- Use `frappe.throw()` for validation errors
- All documents submitted in sequence; failure at any step leaves prior docs submitted (user can cancel)

## Conventions
- Follows existing server script patterns: `frappe.form_dict` for params, `frappe.response` for output
- Uses `frappe.get_doc()` for document creation (not raw SQL)
- Payment always cash, expense account defaults to Cost of Goods Sold
