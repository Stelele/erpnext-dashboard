from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
grouping = frappe.form_dict.get("grouping")
company = frappe.form_dict.get("company")

if not from_date or not to_date or not grouping or not company:
    frappe.throw("from_date, to_date, grouping, and company are required")

format_map = {
    "day": "DATE(posting_date)",
    "week": "DATE(DATE_SUB(posting_date, INTERVAL WEEKDAY(posting_date) DAY))",
    "month": "CONCAT(YEAR(posting_date), '-', LPAD(MONTH(posting_date), 2, '0'))",
    "quarter": "CONCAT(YEAR(posting_date), '-Q', QUARTER(posting_date))",
}

sql_expr = format_map.get(grouping, "DATE(posting_date)")

raw_rows = frappe.db.sql(f"""
    SELECT
        {sql_expr} as grouping_name,
        SUM(total) as total,
        COUNT(*) as count
    FROM `tabPOS Invoice`
    WHERE status IN ('Paid', 'Consolidated')
      AND posting_date BETWEEN %s AND %s
      AND company = %s
    GROUP BY {sql_expr}
    ORDER BY grouping_name
""", (from_date, to_date, company), as_dict=True)

labels = []
data = []

for row in raw_rows:
    labels.append(row.grouping_name)
    data.append(row.total or 0)

frappe.response['data'] = {
    "labels": labels,
    "datasets": [{
        "label": "Sales",
        "data": data
    }]
}
