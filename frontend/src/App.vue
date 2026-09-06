<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { BookOpen, Boxes, Clock3, FileText, LayoutDashboard, LogOut, Search, Settings, Settings2, Star } from 'lucide-vue-next'
import AppearancePanel from './components/AppearancePanel.vue'
import { useAppearanceStore } from './stores/appearance'
import { useAuthStore } from './stores/auth'
import { usePermissionStore } from './stores/permission'
import { useLocale } from './composables/useLocale'

const appearance = useAppearanceStore()
const auth = useAuthStore()
const permission = usePermissionStore()
const route = useRoute()
const router = useRouter()
const appearanceOpen = ref(false)
const { t } = useLocale()
const isLogin = computed(() => route.path === '/login')
const canOpenSystem = computed(() => permission.roles.includes('SUPER_ADMIN') || permission.can('system:view'))

onMounted(async () => {
  appearance.load()
  if (auth.isAuthenticated) {
    try { await permission.load() } catch { permission.reset() }
  }
})

async function logout() {
  permission.reset()
  auth.logout()
  await router.replace('/login')
}
</script>

<template>
  <router-view v-if="isLogin" />
  <div v-else class="shell">
    <div v-if="appearance.settings.watermarkEnabled" class="watermark-layer" aria-hidden="true">
      <span v-for="n in 30" :key="n">{{ appearance.settings.watermarkText }}</span>
    </div>
    <aside class="sidebar">
      <div class="brand"><div class="brand-mark">K</div><div><strong>KnowledgeBase</strong><span>Knowledge Asset Platform</span></div></div>
      <nav>
        <router-link to="/"><LayoutDashboard :size="18"/>{{ t('dashboard') }}</router-link>
        <div class="nav-title">{{ t('knowledgeAssets') }}</div>
        <router-link to="/knowledge-bases"><Boxes :size="18"/>{{ t('myKnowledgeBases') }}</router-link>
        <a><FileText :size="18"/>{{ t('allDocuments') }}</a>
        <a><Clock3 :size="18"/>{{ t('recent') }}</a>
        <a><Star :size="18"/>{{ t('favorites') }}</a>
        <div class="nav-title">{{ t('platform') }}</div>
        <a><BookOpen :size="18"/>{{ t('content') }}</a>
        <router-link v-if="canOpenSystem" to="/system"><Settings :size="18"/>{{ t('system') }}</router-link>
      </nav>
      <div class="sidebar-footer">v0.4.0 · .NET 10 / Vue 3</div>
    </aside>
    <main class="main">
      <header class="topbar">
        <div class="global-search"><Search :size="17"/><span>{{ t('search') }}</span><kbd>Ctrl K</kbd></div>
        <div class="top-actions">
          <button class="icon-button" :title="t('appearance')" @click="appearanceOpen=true"><Settings2 :size="17"/></button>
          <div class="user" :title="auth.currentUser?.displayName || 'User'">{{ (auth.currentUser?.displayName || 'U').slice(0, 1) }}</div>
          <button class="icon-button" title="退出登录" @click="logout"><LogOut :size="16"/></button>
        </div>
      </header>
      <router-view />
    </main>
    <AppearancePanel v-model="appearanceOpen" />
  </div>
</template>
