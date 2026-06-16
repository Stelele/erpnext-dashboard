from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")

# The Query rewritten as a Subquery instead of a CTE to pass safe_exec
query = """
    SELECT
        posting_date,
        current_asset_value AS daily_stock_value,
        /* Calculate days from the end date (1 at end date, n at start date) */
        DATEDIFF(%s, posting_date) + 1 AS days_from_end
    FROM (
        SELECT
            posting_date,
            /* Sum all 'stock_value_difference' chronologically */
            SUM(stock_value_difference) OVER (
                PARTITION BY company 
                ORDER BY posting_date, creation
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
            ) as current_asset_value,
            
            /* Assign a row number to each entry per day, newest first */
            ROW_NUMBER() OVER (
                PARTITION BY posting_date
                ORDER BY creation DESC
            ) as rn
        FROM `tabStock Ledger Entry`
        WHERE
            company = %s
            AND is_cancelled = 0
            AND posting_date <= %s /* Calculate history up to the End Date */
    ) AS RunningBalance
    WHERE 
        rn = 1 /* Only take the final balance entry for each day */
        AND posting_date >= %s /* Only show results for the requested range */
    ORDER BY 
        posting_date
"""

# Parameters reordered to match the new top-to-bottom %s positions
# 1st %s: DATEDIFF (to_date)
# 2nd %s: company
# 3rd %s: posting_date <= (to_date)
# 4th %s: posting_date >= (from_date)
frappe.response['data'] = frappe.db.sql(query, (to_date, company, to_date, from_date), as_dict=True)