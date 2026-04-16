company = frappe.form_dict.get("company")

expense_accounts = [
    "Canteen",
    "Consumables",
    "Entertainment/Director's Expenses",
    "Spoiled Meat",
    "Utility Expenses",
    "Variance Deficit",
    "Miscellaneous Expenses"
]

income_accounts = [
    "Njeremoto Sales",
    "Variance Surplus",
    "Income"
]

income_accounts = [
    ]

def get_bounds(account_name):
    return frappe.db.get_value(
        "Account",
        {
            "company": company,
            "account_name": account_name
        },
        ["account_name", "name"],
        as_dict=True,
    )

frappe.response["data"] = {
    "expense": frappe.get_all(
        "Account",
        filters={
            "company": company, 
            "root_type": "Expense"
        },
        fields=["name", "account_name", "is_group"],
    ),
    "income": frappe.get_all(
        "Account",
        filters={
            "company": company, 
            "root_type": "Income"
        },
        fields=["name", "account_name", "is_group"],
    )
}