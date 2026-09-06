import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'

export type ThemeMode = 'light' | 'dark' | 'system'
export type LocaleCode = 'zh-CN' | 'en-US'

export interface AppearanceSettings {
  theme: ThemeMode
  locale: LocaleCode
  fontSize: number
  watermarkEnabled: boolean
  watermarkText: string
  compactMode: boolean
}

const STORAGE_KEY = 'kb-appearance-settings'
const defaults: AppearanceSettings = {
  theme: 'dark',
  locale: 'zh-CN',
  fontSize: 14,
  watermarkEnabled: false,
  watermarkText: 'KnowledgeBase',
  compactMode: false,
}

export const useAppearanceStore = defineStore('appearance', () => {
  const settings = ref<AppearanceSettings>({ ...defaults })
  const systemDark = ref(window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? true)
  const resolvedTheme = computed(() => settings.value.theme === 'system' ? (systemDark.value ? 'dark' : 'light') : settings.value.theme)

  function load() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (raw) settings.value = { ...defaults, ...JSON.parse(raw) }
    } catch { settings.value = { ...defaults } }
    apply()
  }

  function apply() {
    const root = document.documentElement
    root.dataset.theme = resolvedTheme.value
    root.dataset.compact = settings.value.compactMode ? 'true' : 'false'
    root.style.fontSize = `${settings.value.fontSize}px`
    root.lang = settings.value.locale
  }

  function reset() {
    settings.value = { ...defaults }
  }

  window.matchMedia?.('(prefers-color-scheme: dark)').addEventListener('change', event => {
    systemDark.value = event.matches
    apply()
  })

  watch(settings, value => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(value))
    apply()
  }, { deep: true })

  watch(resolvedTheme, apply)

  return { settings, resolvedTheme, load, apply, reset }
})
