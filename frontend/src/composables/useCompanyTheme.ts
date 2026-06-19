import { ref } from 'vue'
import colors from 'tailwindcss/colors'
import type { PrimaryColor, NeutralColor, ThemeMode } from '@/services/api/schema'
import { useChartColors } from './useChartColors'

const currentPrimaryColor = ref('#111827')

function applyPrimaryPalette(colorName: PrimaryColor | null | undefined): void {
  if (!colorName) {
    currentPrimaryColor.value = '#111827'
    clearPalette('primary')
    return
  }
  if (colorName === 'black') {
    currentPrimaryColor.value = '#000000'
    document.documentElement.style.setProperty('--ui-primary', 'black')
    for (let shade = 50; shade <= 950; shade += 50) {
      document.documentElement.style.removeProperty(`--ui-color-primary-${shade}`)
    }
    return
  }
  const palette = (colors as Record<string, Record<string, string> | string>)[colorName]
  if (typeof palette === 'object') {
    currentPrimaryColor.value = palette['500'] || '#111827'
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
}

function safeSetTheme(value: string): void {
  try {
    localStorage.setItem('vueuse-color-scheme', value)
  } catch {
    // Ignore in private browsing / storage disabled
  }
}

function applyThemeMode(mode: ThemeMode | null | undefined): void {
  if (mode === 'light') {
    document.documentElement.classList.remove('dark')
    document.documentElement.style.colorScheme = 'light'
    safeSetTheme('light')
  } else {
    document.documentElement.classList.add('dark')
    document.documentElement.style.colorScheme = 'dark'
    safeSetTheme('dark')
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
    applyThemeMode(settings?.themeMode ?? undefined)
    await loadChartColors(settings?.primaryColor ?? undefined)
  }

  return { loadAndApply, currentPrimaryColor }
}
