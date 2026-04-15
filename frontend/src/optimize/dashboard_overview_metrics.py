from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")


def get_expense_bounds():
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
        return None

    indirect = get_bounds("Indirect Expenses")
    direct = get_bounds("Direct Expenses")
    stock = get_bounds("Stock Expenses")

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

    return " OR ".join(conditions), tuple(params)


expense_condition, expense_params = get_expense_bounds()

query = f"""
    WITH SalesMetrics AS (
        SELECT 
            COALESCE(SUM(total), 0) AS total_sales,
            COALESCE(COUNT(*), 0) AS nr_sales,
            COALESCE(AVG(total), 0) AS avg_sales
        FROM `tabPOS Invoice`
        WHERE status IN ('Paid', 'Consolidated')
        AND posting_date BETWEEN %s AND %s
        AND company = %s
    ),
    PurchaseMetrics AS (
        SELECT 
            COALESCE(SUM(grand_total), 0) AS total_purchases,
            COALESCE(COUNT(*), 0) AS nr_purchases,
            COALESCE(AVG(grand_total), 0) AS avg_purchases
        FROM `tabPurchase Invoice`
        WHERE docstatus = 1
        AND posting_date BETWEEN %s AND %s
        AND company = %s
    ),
    ExpenseMetrics AS (
        SELECT 
            COALESCE(SUM(gle.debit), 0) AS total_expenses,
            COALESCE(COUNT(*), 0) AS nr_expenses,
            COALESCE(AVG(gle.debit), 0) AS avg_expenses
        FROM `tabGL Entry` gle
        INNER JOIN `tabAccount` acc ON acc.name = gle.account
        WHERE gle.company = %s
        AND gle.posting_date BETWEEN %s AND %s
        AND gle.is_cancelled = 0
        AND ({expense_condition})
    )
    SELECT 
        sm.total_sales,
        sm.nr_sales,
        ROUND(sm.avg_sales, 2) AS avg_sales,
        pm.total_purchases,
        pm.nr_purchases,
        ROUND(pm.avg_purchases, 2) AS avg_purchases,
        em.total_expenses,
        em.nr_expenses,
        ROUND(em.avg_expenses, 2) AS avg_expenses,
        (sm.total_sales - pm.total_purchases) AS gross_profit,
        ROUND(
            CASE 
                WHEN sm.total_sales > 0 THEN ((sm.total_sales - pm.total_purchases) / sm.total_sales * 100)
                ELSE 0
            END, 2
        ) AS gross_margin,
        (sm.total_sales - pm.total_purchases - em.total_expenses) AS net_profit,
        ROUND(
            CASE 
                WHEN sm.total_sales > 0 THEN ((sm.total_sales - pm.total_purchases - em.total_expenses) / sm.total_sales * 100)
                ELSE 0
            END, 2
        ) AS net_margin
    FROM SalesMetrics sm, PurchaseMetrics pm, ExpenseMetrics em
"""

params = (
    from_date, to_date, company,
    from_date, to_date, company,
) + expense_params

frappe.response['data'] = frappe.db.sql(query, params, as_dict=True)
