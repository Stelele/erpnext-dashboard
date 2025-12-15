export interface JournalEntry {
    name: string
    owner: string
    creation: Date
    modified: Date
    modified_by: string
    docstatus: number
    idx: number
    is_system_generated: number
    title: string
    voucher_type: string
    naming_series: string
    company: string
    posting_date: Date
    apply_tds: number
    user_remark: string
    total_debit: number
    total_credit: number
    difference: number
    multi_currency: number
    total_amount: number
    total_amount_in_words: string
    remark: string
    write_off_based_on: string
    write_off_amount: number
    letter_head: string
    party_not_required: number
    is_opening: string
    doctype: string
    accounts: Account[]
}

export interface Account {
    name: string
    owner: string
    creation: Date
    modified: Date
    modified_by: string
    docstatus: number
    idx: number
    account: string
    account_type: string
    cost_center: string
    account_currency: string
    exchange_rate: number
    debit_in_account_currency: number
    debit: number
    credit_in_account_currency: number
    credit: number
    is_advance: string
    against_account: string
    parent: string
    parentfield: string
    parenttype: string
    doctype: string
    __unsaved: number
}
