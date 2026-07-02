import { ref, computed, onMounted, onUnmounted, type Ref, type ComputedRef } from 'vue'

interface UseVisibleWindowOptions {
  rowHeight?: number
  overscan?: number
}

export function useVisibleWindow<T>(
  data: Ref<T[]>,
  wrapperRef: Ref<HTMLElement | null>,
  options: UseVisibleWindowOptions = {}
): {
  visibleRows: ComputedRef<T[]>
  topSpacerHeight: ComputedRef<number>
  bottomSpacerHeight: ComputedRef<number>
} {
  const { rowHeight = 48, overscan = 15 } = options

  const scrollTop = ref(0)
  const containerHeight = ref(0)

  let rafId: number | null = null
  let resizeObserver: ResizeObserver | null = null

  function updateMetrics() {
    if (!wrapperRef.value) return
    scrollTop.value = wrapperRef.value.scrollTop
    containerHeight.value = wrapperRef.value.clientHeight
  }

  function onScroll() {
    if (rafId !== null) return
    rafId = requestAnimationFrame(() => {
      rafId = null
      updateMetrics()
    })
  }

  onMounted(() => {
    if (wrapperRef.value) {
      updateMetrics()
      wrapperRef.value.addEventListener('scroll', onScroll, { passive: true })
      resizeObserver = new ResizeObserver(() => {
        if (wrapperRef.value) {
          containerHeight.value = wrapperRef.value.clientHeight
        }
      })
      resizeObserver.observe(wrapperRef.value)
    }
  })

  onUnmounted(() => {
    if (rafId !== null) {
      cancelAnimationFrame(rafId)
      rafId = null
    }
    if (wrapperRef.value) {
      wrapperRef.value.removeEventListener('scroll', onScroll)
    }
    resizeObserver?.disconnect()
  })

  const visibleRange = computed(() => {
    const rawStart = Math.floor(scrollTop.value / rowHeight)
    const visibleCount = Math.ceil(containerHeight.value / rowHeight)
    const start = Math.max(0, rawStart - overscan)
    const end = Math.min(
      data.value.length,
      rawStart + visibleCount + overscan
    )
    return { start, end }
  })

  const visibleRows = computed(() => {
    const { start, end } = visibleRange.value
    return data.value.slice(start, end)
  })

  const topSpacerHeight = computed(() => {
    return visibleRange.value.start * rowHeight
  })

  const bottomSpacerHeight = computed(() => {
    const { end } = visibleRange.value
    return Math.max(0, (data.value.length - end) * rowHeight)
  })

  return {
    visibleRows,
    topSpacerHeight,
    bottomSpacerHeight,
  }
}
