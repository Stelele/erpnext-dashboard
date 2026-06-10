purchase_invoice = frappe.form_dict.get("purchase_invoice")

if not purchase_invoice:
    frappe.throw("purchase_invoice is required")

if not frappe.db.exists("Purchase Invoice", purchase_invoice):
    frappe.throw(f"Purchase Invoice '{purchase_invoice}' does not exist")

docstatus = frappe.db.get_value("Purchase Invoice", purchase_invoice, "docstatus")
if docstatus != 1:
    frappe.throw(f"Purchase Invoice '{purchase_invoice}' is not in Submitted state")

# Find linked Purchase Receipt
pr_name = frappe.db.get_value("Purchase Invoice Item", {"parent": purchase_invoice}, "purchase_receipt")
if not pr_name:
    frappe.throw("Could not find linked Purchase Receipt")

# Find linked Purchase Order
po_name = frappe.db.get_value("Purchase Receipt Item", {"parent": pr_name}, "purchase_order")
if not po_name:
    frappe.throw("Could not find linked Purchase Order")

# Find linked Payment Entry
pe_name = frappe.db.get_value("Payment Entry Reference", {"reference_doctype": "Purchase Invoice", "reference_name": purchase_invoice}, "parent")

# Cancel in reverse order: Payment Entry -> Purchase Invoice -> Purchase Receipt -> Purchase Order
cancelled = []

if pe_name:
    pe = frappe.get_doc("Payment Entry", pe_name)
    if pe.docstatus == 1:
        pe.cancel()
        cancelled.append(pe_name)

pi = frappe.get_doc("Purchase Invoice", purchase_invoice)
pi.cancel()
cancelled.append(purchase_invoice)

pr = frappe.get_doc("Purchase Receipt", pr_name)
pr.cancel()
cancelled.append(pr_name)

po = frappe.get_doc("Purchase Order", po_name)
po.cancel()
cancelled.append(po_name)

frappe.db.commit()

frappe.response["data"] = {
    "cancelled": cancelled,
    "message": "All purchase documents cancelled successfully",
}
