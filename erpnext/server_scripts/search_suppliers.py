company = frappe.form_dict.get("company")
query = frappe.form_dict.get("query", "")

if not company:
    frappe.throw("Company is required")

filters = {"disabled": 0}
if query:
    filters["supplier_name"] = ["like", f"%{query}%"]

suppliers = frappe.get_all("Supplier", filters=filters, fields=["name", "supplier_name"], limit=20, order_by="supplier_name")

frappe.response["data"] = [
    {"name": s.name, "supplier_name": s.supplier_name}
    for s in suppliers
]
