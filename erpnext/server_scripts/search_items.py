company = frappe.form_dict.get("company")
query = frappe.form_dict.get("query", "")

if not company:
    frappe.throw("Company is required")

filters = {"disabled": 0}
if query:
    filters["item_name"] = ["like", f"%{query}%"]

items = frappe.get_all("Item", filters=filters, fields=["item_code", "item_name"], limit=20, order_by="item_name")

buying_pl = frappe.db.get_value("Buying Settings", None, "buying_price_list") or "Standard Buying"
selling_pl = frappe.db.get_value("Selling Settings", None, "selling_price_list") or "Standard Selling"

result = []
for item in items:
    buy_rate = float(frappe.db.get_value("Item Price", {"item_code": item.item_code, "price_list": buying_pl, "buying": 1}, "price_list_rate") or 0)
    sell_rate = float(frappe.db.get_value("Item Price", {"item_code": item.item_code, "price_list": selling_pl, "selling": 1}, "price_list_rate") or 0)
    result.append({
        "item_code": item.item_code,
        "item_name": item.item_name,
        "last_purchase_rate": buy_rate,
        "last_selling_rate": sell_rate,
        "description": "Buy: " + str(buy_rate) + " | Sell: " + str(sell_rate),
    })

frappe.response["data"] = result
