export interface StockDetail {
  item_code: string;
  item_name: string;
  real_qty: number;
  item_group: string;
  selling_price: number;
  buying_price: number;
  pack_size?: string;
}

export interface StockValueSummary {
  grouping_name: string;
  average_stock_value: number;
  closing_balance: number;
}

export interface DailyStockValue {
  posting_date: string;
  daily_stock_value: number;
  days_from_end: number;
}
