import colors from 'tailwindcss/colors'
import type { PrimaryColor, NeutralColor } from '@/services/api/schema'
import { useChartColors } from './useChartColors'

function applyPrimaryPalette(colorName: PrimaryColor | null | undefined): void {
  if (!colorName) {
    clearPalette('primary')
    return
  }
  if (colorName === 'black') {
    document.documentElement.style.setProperty('--ui-primary', 'black')
    for (let shade = 50; shade <= 950; shade += 50) {
      document.documentElement.style.removeProperty(`--ui-color-primary-${shade}`)
    }
    return
  }
  const palette = (colors as Record<string, Record<string, string> | string>)[colorName]
  if (typeof palette === 'object') {
    for (const [shade, value] of Object.entries(palette)) {
      document.documentElement.style.setProperty(`--ui-color-primary-${shade}`, value)
    }
  }
}

function applyNeutralPalette(colorName: NeutralColor | null | undefined): void {
  if (!colorName) {
    clearPalette('neutral')
    return
  }
  const palette = (colors as Record<string, Record<string, string> | string>)[colorName]
  if (typeof palette === 'object') {
    for (const [shade, value] of Object.entries(palette)) {
      document.documentElement.style.setProperty(`--ui-color-neutral-${shade}`, value)
    }
  }
  // Derive inverted text from neutral-900 with zero chroma to avoid tint clashes
  const neutral900 = palette['900'] as string | undefined
  if (neutral900) {
    const match = neutral900.match(/oklch\(([\d.]+)%\s+[\d.]+\s+[\d.]+\)/)
    if (match) {
      document.documentElement.style.setProperty('--ui-text-inverted', `oklch(${match[1]}% 0 0)`)
    }
  }
}

function clearPalette(semanticName: 'primary' | 'neutral'): void {
  for (let shade = 50; shade <= 950; shade += 50) {
    document.documentElement.style.removeProperty(`--ui-color-${semanticName}-${shade}`)
  }
  if (semanticName === 'primary') {
    document.documentElement.style.removeProperty('--ui-primary')
  }
  if (semanticName === 'neutral') {
    document.documentElement.style.removeProperty('--ui-text-inverted')
  }
}

export function useCompanyTheme() {
  const { loadChartColors } = useChartColors()

  async function loadAndApply(companyId: string): Promise<void> {
    const { useDataStore } = await import('@/stores/DataStore')
    const dataStore = useDataStore()
    const settings = await dataStore.getCompanySettings(companyId)

    applyPrimaryPalette(settings?.primaryColor ?? undefined)
    applyNeutralPalette(settings?.neutralColor ?? undefined)
    await loadChartColors(settings?.primaryColor ?? undefined)
  }

  return { loadAndApply }
}
