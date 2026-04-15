from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")

def get_bounds(account_name):
    result = frappe.db.get_value(
        "Account",
        {
            "company": company,
            "account_name": account_name,
            "is_group": 1,
        },
        ["lft", "rgt"],
        as_dict=False,
    )
    if result:
        return {"lft": result[0], "rgt": result[1]}
    else:
        return None

indirect = get_bounds("Indirect Expenses")
direct = get_bounds("Direct Expenses")
stock = get_bounds("Stock Expenses")

if not indirect and not direct:
    frappe.throw(f"Could not find expense account roots for company {company}")

params = [company, from_date, to_date]
conditions = []

if indirect:
    conditions.append("(acc.lft BETWEEN %s AND %s)")
    params.extend([indirect["lft"], indirect["rgt"]])

if direct:
    if stock:
        conditions.append(
            "(acc.lft BETWEEN %s AND %s AND NOT (acc.lft BETWEEN %s AND %s))"
        )
        params.extend([direct["lft"], direct["rgt"], stock["lft"], stock["rgt"]])
    else:
        conditions.append("(acc.lft BETWEEN %s AND %s)")
        params.extend([direct["lft"], direct["rgt"]])

account_tree_condition = " OR ".join(conditions)

query = """
    SELECT
        gle.account AS expense_type,
        SUM(gle.debit) - SUM(gle.credit) AS total,
        COUNT(gle.name) AS count
    FROM `tabGL Entry` gle
    INNER JOIN `tabAccount` acc ON acc.name = gle.account
    WHERE gle.company = %s
    AND gle.posting_date BETWEEN %s AND %s
    AND gle.is_cancelled = 0
    AND (""" + account_tree_condition + """)
    GROUP BY gle.account
    ORDER BY total DESC
"""

frappe.response['data'] = frappe.db.sql(query, tuple(params), as_dict=True)
