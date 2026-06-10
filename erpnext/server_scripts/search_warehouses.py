company = frappe.form_dict.get("company")

if not company:
    frappe.throw("Company is required")

warehouses = frappe.get_all("Warehouse", filters={"company": company, "is_group": 0}, fields=["name"], order_by="name")

frappe.response["data"] = [{"name": w.name} for w in warehouses]
