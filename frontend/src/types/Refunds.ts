export interface RefundableInvoice {
  name: string;
  customer: string;
  customer_name: string;
  status: string;
  grand_total: number;
  currency: string;
  posting_date: string;
  posting_time: string;
  paid_amount: number;
  total_qty: number;
  owner: string;
}

export interface RefundableItem {
  name: string;
  item_code: string;
  item_name: string;
  qty: number;
  returnable_qty: number;
  uom: string;
  rate: number;
  amount: number;
}

export interface RefundTax {
  description: string;
  tax_amount_after_discount_amount: number;
}

export interface RefundPayment {
  mode_of_payment: string;
  amount: number;
}

export interface RefundInvoiceDetail {
  name: string;
  customer: string;
  customer_name: string;
  status: string;
  posting_date: string;
  posting_time: string;
  paid_amount: number;
  owner: string;
  currency: string;
  net_total: number;
  grand_total: number;
  discount_amount: number;
  additional_discount_percentage: number;
  total_qty: number;
  items: RefundableItem[];
  taxes: RefundTax[];
  payments: RefundPayment[];
}

export interface RefundHistoryItem {
  name: string;
  customer: string;
  customer_name: string;
  return_against: string | null;
  grand_total: number;
  currency: string;
  posting_date: string;
  posting_time: string;
  owner: string;
}

export interface ReturnItemPayload {
  item_name: string;
  qty: number;
}
