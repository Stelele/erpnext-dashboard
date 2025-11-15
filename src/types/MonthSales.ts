export interface GroupSummary {
    grouping_name: string;
    total: number;
    count: number;
}

export interface ItemGroupSummary extends GroupSummary {
    item_group: string;
}
