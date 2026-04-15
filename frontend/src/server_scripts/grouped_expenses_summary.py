from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")
time_grouping = frappe.form_dict.get("time_grouping")


if not from_date or not to_date or not company:
    frappe.throw("from_date, to_date and company are required")

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
        # result is a tuple like (lft, rgt)
        return {"lft": result[0], "rgt": result[1]}
    else:
        return None

indirect = get_bounds("Indirect Expenses")
direct = get_bounds("Direct Expenses")
stock = get_bounds("Stock Expenses")

if not indirect and not direct:
    frappe.throw(f"Could not find 'Indirect Expenses' or 'Direct Expenses' roots for company {company}")

params = [company, from_date, to_date]
conditions = []

# 1) All children of Indirect Expenses
if indirect:
    conditions.append("(acc.lft BETWEEN %s AND %s)")
    params.extend([indirect["lft"], indirect["rgt"]])

# 2) Direct Expenses children, excluding Stock Expenses subtree
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

query = f"""
    SELECT
        DATE_FORMAT(gle.posting_date, '{time_grouping}') AS grouping_name,
        SUM(gle.debit) - SUM(gle.credit) AS total,
        COUNT(gle.name) AS count,
        gle.account AS account
    FROM `tabGL Entry` gle
    INNER JOIN `tabAccount` acc
        ON acc.name = gle.account
    WHERE
        gle.company = %s
        AND gle.posting_date BETWEEN %s AND %s
        AND gle.is_cancelled = 0
        AND ({account_tree_condition})
    GROUP BY
        DATE_FORMAT(gle.posting_date, '{time_grouping}')
    ORDER BY
        grouping_name ASC
"""

frappe.response['data'] = frappe.db.sql(query, tuple(params), as_dict=True)