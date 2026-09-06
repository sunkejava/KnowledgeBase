<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bell, BookOpen, Boxes, Clock3, Download, FileSearch, FileText, LayoutDashboard, LogOut, MessageSquareText, Search, Settings, Settings2, Star } from 'lucide-vue-next'
import AppearancePanel from './components/AppearancePanel.vue'
import { useAppearanceStore } from './stores/appearance'
import { useAuthStore } from './stores/auth'
import { usePermissionStore } from './stores/permission'
import { useNotificationStore } from './stores/notifications'
import { useLocale } from './composables/useLocale'

const appearance = useAppearanceStore()
const auth = useAuthStore()
const permission = usePermissionStore()
const notifications = useNotificationStore()
const route = useRoute()
const router = useRouter()
const appearanceOpen = ref(false)
const { t } = useLocale()
const isPublicPage = computed(() => Boolean(route.meta.public))
const canOpenSystem = computed(() => permission.roles.includes('SUPER_ADMIN') || permission.can('system:view'))
const isSuperAdmin = computed(() => permission.roles.includes('SUPER_ADMIN'))

/**
 * 左侧导航使用明确的 path + query 判断激活状态。
 * Vue Router 默认 active 只按路由记录匹配，/knowledge-center 下不同 tab 会同时高亮，因此不能直接依赖 router-link-active。
 */
function isNavActive(path: string, tab?: string) {
  if (path === '/') return route.path === '/'
  if (path === '/knowledge-bases') return route.path === '/knowledge-bases' || route.path.startsWith('/knowledge-bases/')
  if (path === '/knowledge-center') return route.path === path && String(route.query.tab || 'search') === String(tab || 'search')
  return route.path === path || route.path.startsWith(`${path}/`)
}

onMounted(async () => {
  appearance.load()
  if (auth.isAuthenticated) {
    try {
      await Promise.all([permission.load(), notifications.refresh()])
    } catch {
      permission.reset()
      notifications.reset()
    }
  }
})

async function logout() {
  permission.reset()
  notifications.reset()
  auth.logout()
  await router.replace('/login')
}
</script>

<template>
  <router-view v-if="isPublicPage"/>
  <div v-else class="shell">
    <div v-if="appearance.settings.watermarkEnabled" class="watermark-layer" aria-hidden="true"><span v-for="n in 30" :key="n">{{appearance.settings.watermarkText}}</span></div>
    <aside class="sidebar">
      <div class="brand"><div class="brand-mark">K</div><div><strong>KnowledgeBase</strong><span>Knowledge Asset Platform</span></div></div>
      <nav>
        <router-link to="/" :class="{ 'nav-active': isNavActive('/') }"><LayoutDashboard :size="18"/>{{t('dashboard')}}</router-link>
        <div class="nav-title">{{t('knowledgeAssets')}}</div>
        <router-link to="/knowledge-bases" :class="{ 'nav-active': isNavActive('/knowledge-bases') }"><Boxes :size="18"/>{{t('myKnowledgeBases')}}</router-link>
        <router-link :to="{path:'/knowledge-center',query:{tab:'search'}}" :class="{ 'nav-active': isNavActive('/knowledge-center','search') }"><FileText :size="18"/>{{t('allDocuments')}}</router-link>
        <router-link :to="{path:'/knowledge-center',query:{tab:'recent'}}" :class="{ 'nav-active': isNavActive('/knowledge-center','recent') }"><Clock3 :size="18"/>{{t('recent')}}</router-link>
        <router-link :to="{path:'/knowledge-center',query:{tab:'favorites'}}" :class="{ 'nav-active': isNavActive('/knowledge-center','favorites') }"><Star :size="18"/>{{t('favorites')}}</router-link>
        <router-link to="/collaboration" :class="{ 'nav-active': isNavActive('/collaboration') }"><MessageSquareText :size="18"/>{{t('collaboration')}}</router-link>
        <div class="nav-title">{{t('platform')}}</div>
        <router-link :to="{path:'/knowledge-center',query:{tab:'tags'}}" :class="{ 'nav-active': isNavActive('/knowledge-center','tags') }"><BookOpen :size="18"/>{{t('content')}}</router-link>
        <router-link to="/notifications" :class="{ 'nav-active': isNavActive('/notifications') }"><Bell :size="18"/>{{t('notifications')}}<span v-if="notifications.unreadCount" style="margin-left:auto;font-size:11px">{{notifications.unreadCount > 99 ? '99+' : notifications.unreadCount}}</span></router-link>
        <router-link to="/data-exchange" :class="{ 'nav-active': isNavActive('/data-exchange') }"><Download :size="18"/>{{t('dataExchange')}}</router-link>
        <router-link v-if="isSuperAdmin" to="/search-management" :class="{ 'nav-active': isNavActive('/search-management') }"><FileSearch :size="18"/>{{t('searchManagement')}}</router-link>
        <router-link v-if="canOpenSystem" to="/system" :class="{ 'nav-active': isNavActive('/system') }"><Settings :size="18"/>{{t('system')}}</router-link>
      </nav>
      <div class="sidebar-footer">v0.14.2 · .NET 10 / Vue 3</div>
    </aside>
    <main class="main">
      <header class="topbar">
        <div class="global-search" @click="router.push('/knowledge-center')"><Search :size="17"/><span>{{t('search')}}</span><kbd>Ctrl K</kbd></div>
        <div class="top-actions">
          <button class="icon-button" :title="t('notifications')" @click="router.push('/notifications')"><Bell :size="17"/><sup v-if="notifications.unreadCount" style="font-size:10px">{{notifications.unreadCount > 9 ? '9+' : notifications.unreadCount}}</sup></button>
          <button class="icon-button" :title="t('appearance')" @click="appearanceOpen=true"><Settings2 :size="17"/></button>
          <div class="user" :title="auth.currentUser?.displayName||'User'">{{(auth.currentUser?.displayName||'U').slice(0,1)}}</div>
          <button class="icon-button" title="退出登录" @click="logout"><LogOut :size="16"/></button>
        </div>
      </header>
      <router-view/>
    </main>
    <AppearancePanel v-model="appearanceOpen"/>
  </div>
</template>
