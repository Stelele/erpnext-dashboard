from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")
time_grouping = frappe.form_dict.get("time_grouping")

# 2. The Query rewritten with nested subqueries and window functions
query = f"""
    SELECT
        grouping_name,
        /* Average the balances found in that period */
        AVG(current_asset_value) AS average_stock_value,
        /* The closing balance is identical for all rows in the group now, 
           so MAX() just cleanly extracts it for the GROUP BY */
        MAX(closing_balance) AS closing_balance
    FROM (
        SELECT
            posting_date,
            current_asset_value,
            DATE_FORMAT(posting_date, '{time_grouping}') AS grouping_name,
            
            /* Grab the last asset value for this specific time group */
            FIRST_VALUE(current_asset_value) OVER (
                PARTITION BY DATE_FORMAT(posting_date, '{time_grouping}') 
                ORDER BY posting_date DESC, creation DESC
            ) AS closing_balance
            
        FROM (
            SELECT
                posting_date,
                creation,
                /* The Magic Line:
                   Sum all 'stock_value_difference' from the beginning of time 
                   up to the current row. This recreates the balance at that exact moment.
                */
                SUM(stock_value_difference) OVER (
                    PARTITION BY company 
                    ORDER BY posting_date, creation
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS current_asset_value
            FROM `tabStock Ledger Entry`
            WHERE
                company = %s
                AND is_cancelled = 0
                AND posting_date <= %s /* Calculate history up to the End Date */
        ) AS BaseBalance
        
        WHERE 
            posting_date >= %s /* Only show results for the requested range */
            
    ) AS GroupedBalance
    
    GROUP BY 
        grouping_name
    ORDER BY 
        grouping_name
"""

# The parameter order remains exactly the same!
# 1st %s: company
# 2nd %s: posting_date <= (to_date)
# 3rd %s: posting_date >= (from_date)
frappe.response['data'] = frappe.db.sql(query, (company, to_date, from_date), as_dict=True)