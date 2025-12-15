import type { NumberCardProps } from "../components/NumberCard.vue";

export type CardData = Omit<NumberCardProps, 'title'> & { prevValue?: number }