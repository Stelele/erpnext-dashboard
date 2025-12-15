import type { ChangeDirection } from "../components/NumberCard.vue"

export function calculatePercentChange(curValue: number, prevValue?: number) {
    if (!prevValue) {
        return undefined
    }

    const percentChange = Math.round((curValue - prevValue) / prevValue * 100)
    const direction: ChangeDirection = percentChange > 0
        ? 'up'
        : percentChange < 0
            ? 'down'
            : 'flat'

    return {
        percentChange: Math.abs(percentChange),
        direction,
    }
}