company = frappe.form_dict.get("company")

frappe.response["data"] = {
    "expense": frappe.get_all(
        "Account",
        filters={
            "company": company,
            "root_type": "Expense",
            "is_group": 0,
        },
        fields=["name", "account_name"],
    ),
    "income": frappe.get_all(
        "Account",
        filters={
            "company": company,
            "root_type": "Income",
            "is_group": 0,
        },
        fields=["name", "account_name"],
    )
}
