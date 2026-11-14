export function useLatestCall(): {
    start: () => number;
    isCurrent: (seq: number) => boolean;
} {
    let seq = 0;

    function start(): number {
        seq += 1;
        return seq;
    }

    function isCurrent(candidate: number): boolean {
        return candidate === seq;
    }

    return { start, isCurrent };
}
