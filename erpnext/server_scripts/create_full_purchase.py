def validate_inputs(company, supplier, warehouse, items, invoice_number, invoice_date):
    """Validate all required inputs exist and are valid."""
    if not company:
        frappe.throw("Company is required")
    if not frappe.db.exists("Company", company):
        frappe.throw(f"Company '{company}' does not exist")

    if not supplier:
        frappe.throw("Supplier is required")
    if not frappe.db.exists("Supplier", supplier):
        frappe.throw(f"Supplier '{supplier}' does not exist")

    if not warehouse:
        frappe.throw("Warehouse is required")
    if not frappe.db.exists("Warehouse", warehouse):
        frappe.throw(f"Warehouse '{warehouse}' does not exist")

    if not items or not isinstance(items, list) or len(items) == 0:
        frappe.throw("At least one item is required")

    for item in items:
        if not isinstance(item, dict):
            frappe.throw("Each item must be a dictionary with item_code, qty, and rate")
        if not item.get("item_code"):
            frappe.throw("item_code is required for each item")
        if not frappe.db.exists("Item", item["item_code"]):
            frappe.throw(f"Item '{item['item_code']}' does not exist")
        if not item.get("qty") or float(item["qty"] or 0) <= 0:
            frappe.throw(f"qty must be greater than 0 for item '{item['item_code']}'")
        if item.get("rate") is None or float(item["rate"] or 0) < 0:
            frappe.throw(f"rate must be 0 or greater for item '{item['item_code']}'")

    # Check for duplicate invoice only if number provided
    if invoice_number:
        existing = frappe.db.exists("Purchase Invoice", {"bill_no": invoice_number, "supplier": supplier, "docstatus": 1})
        if existing:
            frappe.throw(f"Purchase Invoice with invoice_number '{invoice_number}' already exists for this supplier")




def create_purchase_order(company, supplier, warehouse, items, invoice_date):
    """Create and submit a Purchase Order."""
    po = frappe.get_doc({
        "doctype": "Purchase Order",
        "company": company,
        "supplier": supplier,
        "transaction_date": invoice_date,
        "schedule_date": invoice_date,
        "set_warehouse": warehouse,
        "items": [
            {
                "item_code": item["item_code"],
                "qty": float(item["qty"] or 0),
                "rate": float(item["rate"] or 0),
                "warehouse": warehouse,
            }
            for item in items
        ],
    })
    po.insert()
    po.submit()
    return po


def create_purchase_receipt(po, invoice_date):
    """Create and submit a Purchase Receipt from a Purchase Order."""
    pr = frappe.get_doc({
        "doctype": "Purchase Receipt",
        "company": po.company,
        "supplier": po.supplier,
        "posting_date": invoice_date,
        "posting_time": "00:00:00",
        "set_warehouse": po.set_warehouse,
        "purchase_order": po.name,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_order": po.name,
                "purchase_order_item": item.name,
            }
            for item in po.items
        ],
    })
    pr.insert()
    pr.submit()
    return pr


def create_purchase_invoice(pr, invoice_number, invoice_date):
    """Create and submit a Purchase Invoice from a Purchase Receipt with user's invoice details."""
    pi_data = {
        "doctype": "Purchase Invoice",
        "company": pr.company,
        "supplier": pr.supplier,
        "posting_date": invoice_date,
        "posting_time": "00:00:00",
        "bill_date": invoice_date,
        "purchase_receipt": pr.name,
        "update_stock": 0,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_order": item.purchase_order,
                "po_detail": item.purchase_order_item,
                "purchase_receipt": pr.name,
                "pr_detail": item.name,
            }
            for item in pr.items
        ],
    }
    if invoice_number:
        pi_data["bill_no"] = invoice_number
    pi = frappe.get_doc(pi_data)
    pi.insert()
    pi.submit()
    return pi


def create_payment_entry(pi, invoice_date):
    """Create and submit a cash Payment Entry against the Purchase Invoice."""
    company = pi.company

    # Get supplier name for party_name
    supplier_name = frappe.db.get_value("Supplier", pi.supplier, "supplier_name") or pi.supplier

    # Get default cash account from company
    default_cash_account = frappe.db.get_value("Company", company, "default_cash_account")
    if not default_cash_account:
        # Fallback: find any cash account
        default_cash_account = frappe.db.get_value(
            "Account",
            {"company": company, "account_type": "Cash", "is_group": 0},
            "name"
        )

    if not default_cash_account:
        frappe.throw(f"No default cash account found for company '{company}'")

    # Get default payable account
    default_payable = frappe.db.get_value("Company", company, "default_payable_account")
    if not default_payable:
        default_payable = frappe.db.get_value(
            "Account",
            {"company": company, "account_type": "Payable", "is_group": 0},
            "name"
        )

    if not default_payable:
        frappe.throw(f"No default payable account found for company '{company}'")

    # Verify Cash mode of payment exists
    if not frappe.db.exists("Mode of Payment", "Cash"):
        frappe.throw("Mode of Payment 'Cash' does not exist. Please create it first.")

    pe = frappe.get_doc({
        "doctype": "Payment Entry",
        "company": company,
        "payment_type": "Pay",
        "party_type": "Supplier",
        "party": pi.supplier,
        "party_name": supplier_name,
        "posting_date": invoice_date,
        "mode_of_payment": "Cash",
        "paid_from": default_cash_account,
        "paid_to": default_payable,
        "paid_amount": pi.grand_total,
        "received_amount": pi.grand_total,
        "reference_no": pi.bill_no or pi.name,
        "reference_date": pi.bill_date,
        "references": [
            {
                "reference_doctype": "Purchase Invoice",
                "reference_name": pi.name,
                "total_amount": pi.grand_total,
                "outstanding_amount": pi.outstanding_amount,
                "allocated_amount": pi.grand_total,
                "exchange_rate": 1,
            }
        ],
    })
    pe.insert()
    pe.submit()
    return pe


# --- Main entry point (Frappe Server Script pattern) ---

# Parse inputs
company = frappe.form_dict.get("company")
supplier = frappe.form_dict.get("supplier")
warehouse = frappe.form_dict.get("warehouse")
invoice_number = frappe.form_dict.get("invoice_number")
invoice_date = frappe.form_dict.get("invoice_date")

# Defaults
if not invoice_date:
    invoice_date = frappe.utils.nowdate()

# Parse items from request body (Frappe auto-parses JSON body into form_dict)
items = frappe.form_dict.get("items", [])

# Validate
validate_inputs(company, supplier, warehouse, items, invoice_number, invoice_date)

# Execute purchase cycle with transaction rollback
try:
    po = create_purchase_order(company, supplier, warehouse, items, invoice_date)
    pr = create_purchase_receipt(po, invoice_date)
    pi = create_purchase_invoice(pr, invoice_number, invoice_date)
    pe = create_payment_entry(pi, invoice_date)

    # Correct posting_date on docs where Frappe's set_posting_time may override it
    for doc, doctype in [(pr, "Purchase Receipt"), (pi, "Purchase Invoice"), (pe, "Payment Entry")]:
        frappe.db.set_value(doctype, doc.name, "posting_date", invoice_date)

    frappe.db.commit()
except Exception:
    frappe.db.rollback()
    raise

# Update item prices for future purchases
buying_pl = frappe.db.get_value("Buying Settings", None, "buying_price_list") or "Standard Buying"
selling_pl = frappe.db.get_value("Selling Settings", None, "selling_price_list") or "Standard Selling"

for item in items:
    buy_rate = float(item.get("rate") or 0)
    if buy_rate > 0:
        existing_buy = frappe.db.exists("Item Price", {"item_code": item["item_code"], "price_list": buying_pl, "buying": 1})
        if existing_buy:
            frappe.db.set_value("Item Price", existing_buy, "price_list_rate", buy_rate)
        else:
            frappe.get_doc({
                "doctype": "Item Price",
                "item_code": item["item_code"],
                "price_list": buying_pl,
                "buying": 1,
                "price_list_rate": buy_rate,
            }).insert()

    sell_rate = float(item.get("sell_rate") or 0)
    if sell_rate > 0:
        existing_sell = frappe.db.exists("Item Price", {"item_code": item["item_code"], "price_list": selling_pl, "selling": 1})
        if existing_sell:
            frappe.db.set_value("Item Price", existing_sell, "price_list_rate", sell_rate)
        else:
            frappe.get_doc({
                "doctype": "Item Price",
                "item_code": item["item_code"],
                "price_list": selling_pl,
                "selling": 1,
                "price_list_rate": sell_rate,
            }).insert()

# Return result
frappe.response["data"] = {
    "purchase_order": po.name,
    "purchase_receipt": pr.name,
    "purchase_invoice": pi.name,
    "payment_entry": pe.name,
}
