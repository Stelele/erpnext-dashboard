import { setChartColors } from '@/utils/ChartJsColors'
import { CachedApiClient } from '@/services/cache/CachedApiClient'
import type { PrimaryColor } from '@/services/api/schema'

export function useChartColors() {
  async function loadChartColors(primaryColor: PrimaryColor | null | undefined): Promise<void> {
    if (!primaryColor) {
      setChartColors([])
      return
    }

    try {
      const client = CachedApiClient.getInstance()
      const colors = await client.getChartColors(primaryColor)
      if (colors && colors.length > 0) {
        setChartColors(colors)
      }
    } catch {
      setChartColors([])
    }
  }

  return { loadChartColors }
}
