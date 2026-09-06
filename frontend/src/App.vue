<script setup lang="ts">
import { BookOpen, Boxes, Clock3, FileText, LayoutDashboard, Search, Settings, Star } from 'lucide-vue-next'
</script>

<template>
  <div class="shell">
    <aside class="sidebar">
      <div class="brand"><div class="brand-mark">K</div><div><strong>KnowledgeBase</strong><span>知识资产平台</span></div></div>
      <nav>
        <router-link to="/"><LayoutDashboard :size="18"/>工作台</router-link>
        <div class="nav-title">知识资产</div>
        <router-link to="/knowledge-bases"><Boxes :size="18"/>我的知识库</router-link>
        <a><FileText :size="18"/>全部文档</a>
        <a><Clock3 :size="18"/>最近浏览</a>
        <a><Star :size="18"/>我的收藏</a>
        <div class="nav-title">平台</div>
        <a><BookOpen :size="18"/>内容管理</a>
        <a><Settings :size="18"/>系统管理</a>
      </nav>
      <div class="sidebar-footer">v0.1.0 · .NET 10 / Vue 3</div>
    </aside>
    <main class="main">
      <header class="topbar"><div class="global-search"><Search :size="17"/><span>搜索知识库、文档、标签</span><kbd>Ctrl K</kbd></div><div class="user">AD</div></header>
      <router-view />
    </main>
  </div>
</template>
