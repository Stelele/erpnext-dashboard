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
    rate = float(frappe.db.get_value("Item Price", {"item_code": item.item_code, "price_list": default_price_list, "buying": 1}, "price_list_rate") or 0)
    result.append({
        "item_code": item.item_code,
        "item_name": item.item_name,
        "last_purchase_rate": rate,
    })

frappe.response["data"] = result
