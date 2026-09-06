import { computed } from 'vue'
import { messages } from '../i18n/messages'
import { useAppearanceStore } from '../stores/appearance'

export function useLocale() {
  const appearance = useAppearanceStore()
  const t = (key: keyof typeof messages['zh-CN']) => computed(() => messages[appearance.settings.locale][key]).value
  return { t }
}
