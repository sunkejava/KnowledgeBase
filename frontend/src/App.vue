<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { BookOpen, Boxes, Clock3, FileText, LayoutDashboard, Search, Settings, Settings2, Star } from 'lucide-vue-next'
import AppearancePanel from './components/AppearancePanel.vue'
import { useAppearanceStore } from './stores/appearance'
import { useLocale } from './composables/useLocale'

const appearance = useAppearanceStore()
const appearanceOpen = ref(false)
const { t } = useLocale()
onMounted(() => appearance.load())
</script>

<template>
  <div class="shell">
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
        <a><Settings :size="18"/>{{ t('system') }}</a>
      </nav>
      <div class="sidebar-footer">v0.2.0 · .NET 10 / Vue 3</div>
    </aside>
    <main class="main">
      <header class="topbar">
        <div class="global-search"><Search :size="17"/><span>{{ t('search') }}</span><kbd>Ctrl K</kbd></div>
        <div class="top-actions"><button class="icon-button" :title="t('appearance')" @click="appearanceOpen=true"><Settings2 :size="17"/></button><div class="user">AD</div></div>
      </header>
      <router-view />
    </main>
    <AppearancePanel v-model="appearanceOpen" />
  </div>
</template>
