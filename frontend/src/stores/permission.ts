import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi } from '../api/modules/auth'

export const usePermissionStore = defineStore('permission', () => {
  const roles = ref<string[]>([])
  const permissions = ref<string[]>([])
  const menus = ref<any[]>([])

  async function load() {
    const { data } = await authApi.permissionProfile()
    roles.value = data.roles
    permissions.value = data.permissions
    menus.value = data.menus
  }

  function can(code: string) {
    return roles.value.includes('SUPER_ADMIN') || permissions.value.includes(code)
  }

  function reset() {
    roles.value = []
    permissions.value = []
    menus.value = []
  }

  return { roles, permissions, menus, load, can, reset }
})
