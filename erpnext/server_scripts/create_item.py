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

if not frappe.db.exists("Item Group", item_group):
    frappe.throw(f"Item group '{item_group}' does not exist")

# Check for duplicate item_code
if frappe.db.exists("Item", item_name):
    frappe.throw(f"Item '{item_name}' already exists")

try:
    # Create Item doctype
    item = frappe.get_doc({
        "doctype": "Item",
        "item_code": item_name,
        "item_name": item_name,
        "item_group": item_group,
        "stock_uom": "Nos",
        "is_stock_item": 1,
        "is_purchase_item": 1,
        "is_sales_item": 1,
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

    frappe.db.commit()
except Exception:
    frappe.db.rollback()
    raise

frappe.response["data"] = {
    "item_code": item_name,
    "item_name": item_name,
    "last_purchase_rate": buying_price,
    "last_selling_rate": selling_price,
    "description": "",
}
