import { setChartColors } from '@/utils/ChartJsColors'
import { ApiSingleton } from '@/services/api'
import type { PrimaryColor } from '@/services/api/schema'

let idbKeyval: typeof import('idb-keyval') | null = null
async function getKeyval() {
  if (!idbKeyval) {
    idbKeyval = await import('idb-keyval')
  }
  return idbKeyval
}

async function getCached(key: string): Promise<string[] | undefined> {
  try {
    const kv = await getKeyval()
    return await kv.get(key)
  } catch {
    return undefined
  }
}

async function setCached(key: string, value: string[]): Promise<void> {
  try {
    const kv = await getKeyval()
    await kv.set(key, value)
  } catch {
    // Silently fail — cache is optional
  }
}

export function useChartColors() {
  async function loadChartColors(primaryColor: PrimaryColor | null | undefined): Promise<void> {
    if (!primaryColor) {
      setChartColors([])
      return
    }

    const cacheKey = `chart-colors:${primaryColor}`

    const cached = await getCached(cacheKey)
    if (cached && cached.length > 0) {
      setChartColors(cached)
      return
    }

    try {
      const api = await ApiSingleton.getInstance()
      const { data, error } = await api.GET('/api/theme/chart-colors', {
        params: { query: { primaryColor } },
      })
      if (data?.chartColors?.length) {
        setChartColors(data.chartColors as string[])
        await setCached(cacheKey, data.chartColors as string[])
      }
    } catch {
      // Fallback: keep defaults set by setChartColors([])
    }
  }

  return { loadChartColors }
}
