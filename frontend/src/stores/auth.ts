import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { http } from '../api/http'

export interface CurrentUser {
  id: string
  userName: string
  displayName: string
  roles: string[]
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('kb_access_token') || '')
  const currentUser = ref<CurrentUser | null>(JSON.parse(localStorage.getItem('kb_current_user') || 'null'))
  const isAuthenticated = computed(() => Boolean(token.value))

  async function login(userName: string, password: string) {
    const { data } = await http.post('/auth/login', { userName, password })
    token.value = data.accessToken
    currentUser.value = data.user
    localStorage.setItem('kb_access_token', data.accessToken)
    localStorage.setItem('kb_current_user', JSON.stringify(data.user))
  }

  function logout() {
    token.value = ''
    currentUser.value = null
    localStorage.removeItem('kb_access_token')
    localStorage.removeItem('kb_current_user')
  }

  return { token, currentUser, isAuthenticated, login, logout }
})
