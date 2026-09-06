import { defineStore } from 'pinia'
import { ref } from 'vue'
import { collaborationApi } from '../api/modules/collaboration'

export const useNotificationStore = defineStore('notifications', () => {
  const unreadCount = ref(0)
  const loading = ref(false)

  async function refresh() {
    loading.value = true
    try {
      const { data } = await collaborationApi.notificationSummary()
      unreadCount.value = Number(data.unreadCount || 0)
    } finally {
      loading.value = false
    }
  }

  function reset() {
    unreadCount.value = 0
  }

  return { unreadCount, loading, refresh, reset }
})
