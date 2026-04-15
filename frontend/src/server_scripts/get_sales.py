# Get parameters from the request
from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")

# Validation
if not from_date or not to_date or not company:
    frappe.throw("Please provide from_date date, to_date date, and company name.")

# Execute Efficient SQL Query
data = frappe.db.sql("""
    SELECT
        parent.name AS pos_invoice_id,
        parent.posting_date,
        child.item_name,
        child.item_group,
        child.qty,
        child.rate,
        (child.qty * child.rate) as item_total_amount
    FROM
        `tabPOS Invoice` parent
    JOIN
        `tabPOS Invoice Item` child ON child.parent = parent.name
    WHERE
        parent.status IN ('Paid', 'Consolidated')
        AND parent.company = %s
        AND parent.posting_date BETWEEN %s AND %s
    ORDER BY
        parent.posting_date DESC, parent.name
""", (company, from_date, to_date), as_dict=True)

frappe.response["data"] = data