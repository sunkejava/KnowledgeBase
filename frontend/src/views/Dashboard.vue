<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { dashboardApi, type DashboardOverview } from '../api/modules/dashboard'

const router = useRouter()
const loading = ref(false)
const overview = ref<DashboardOverview>({
  knowledgeBaseCount: 0,
  documentCount: 0,
  favoriteCount: 0,
  unreadNotificationCount: 0,
  memberCount: 0,
  failedTaskCount: 0,
  recentDocuments: [],
  popularDocuments: [],
  taskSummaries: []
})

const pendingTaskCount = computed(() => overview.value.taskSummaries.reduce((sum, item) => sum + item.pending, 0))
const runningTaskCount = computed(() => overview.value.taskSummaries.reduce((sum, item) => sum + item.running, 0))

function formatTime(value: string) {
  if (!value) return '-'
  return new Date(value).toLocaleString()
}

async function load() {
  loading.value = true
  try {
    overview.value = (await dashboardApi.overview()).data
  } finally {
    loading.value = false
  }
}

function openDocument(knowledgeBaseId: string, documentId: string) {
  void router.push({ path: `/knowledge-bases/${knowledgeBaseId}`, query: { document: documentId } })
}

onMounted(load)
</script>

<template>
  <section class="page" v-loading="loading">
    <div class="page-head">
      <div><h1>工作台</h1><p>基于当前账号权限实时统计知识资产、访问记录、通知与后台任务。</p></div>
      <button class="primary" @click="router.push('/knowledge-bases')">进入知识库</button>
    </div>

    <div class="metrics">
      <div class="metric"><span>可访问文档</span><strong>{{ overview.documentCount.toLocaleString() }}</strong><small>来自当前账号有权访问的知识库</small></div>
      <div class="metric"><span>知识库</span><strong>{{ overview.knowledgeBaseCount.toLocaleString() }}</strong><small>权限范围内空间总数</small></div>
      <div class="metric"><span>我的收藏</span><strong>{{ overview.favoriteCount.toLocaleString() }}</strong><small>已收藏且仍有权限访问</small></div>
      <div class="metric"><span>未读通知</span><strong>{{ overview.unreadNotificationCount.toLocaleString() }}</strong><small>{{ overview.memberCount }} 名相关知识库成员</small></div>
    </div>

    <div class="grid dashboard-grid">
      <div class="panel wide">
        <div class="panel-title"><strong>最近访问</strong><button class="link-button" @click="router.push({path:'/knowledge-center',query:{tab:'recent'}})">查看全部</button></div>
        <div v-if="!overview.recentDocuments.length" class="panel-empty">暂无最近访问记录</div>
        <button
          v-for="item in overview.recentDocuments"
          :key="item.documentId"
          class="doc-row dashboard-doc-row"
          @click="openDocument(item.knowledgeBaseId, item.documentId)"
        >
          <div><b>{{ item.title }}</b><span>{{ item.knowledgeBaseName }} · 访问 {{ item.viewCount }} 次</span></div>
          <time>{{ formatTime(item.lastViewedAt) }}</time>
        </button>
      </div>

      <div class="panel">
        <div class="panel-title"><strong>热门知识</strong><span>累计访问</span></div>
        <div v-if="!overview.popularDocuments.length" class="panel-empty">暂无访问统计</div>
        <ol v-else>
          <li v-for="item in overview.popularDocuments" :key="item.documentId">
            <button class="popular-link" @click="openDocument(item.knowledgeBaseId, item.documentId)">{{ item.title }}</button>
            <em>{{ item.viewCount.toLocaleString() }}</em>
          </li>
        </ol>
      </div>

      <div class="panel wide">
        <div class="panel-title"><strong>后台任务</strong><span>实时状态</span></div>
        <div class="task-summary-grid">
          <div v-for="item in overview.taskSummaries" :key="item.type" class="task-summary-card">
            <b>{{ item.type }}</b>
            <span>等待 {{ item.pending }}</span>
            <span>运行 {{ item.running }}</span>
            <span :class="{ danger: item.failed > 0 }">失败 {{ item.failed }}</span>
          </div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-title"><strong>需要关注</strong><span>系统摘要</span></div>
        <div class="todo"><b>{{ pendingTaskCount }}</b><span>等待执行任务</span></div>
        <div class="todo"><b>{{ runningTaskCount }}</b><span>正在运行任务</span></div>
        <div class="todo"><b :class="{ danger: overview.failedTaskCount > 0 }">{{ overview.failedTaskCount }}</b><span>失败任务</span></div>
        <div class="todo"><b>{{ overview.unreadNotificationCount }}</b><span>未读通知</span></div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.dashboard-grid{align-items:stretch}.link-button,.popular-link,.dashboard-doc-row{border:0!important;background:transparent!important}.link-button{height:auto!important;padding:0!important;color:var(--accent)!important;cursor:pointer}.dashboard-doc-row{width:100%;height:58px!important;padding:0!important;text-align:left;cursor:pointer;color:var(--text)!important}.dashboard-doc-row:hover{background:var(--hover)!important}.dashboard-doc-row>div{min-width:0;display:flex;align-items:center}.dashboard-doc-row b{max-width:360px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.popular-link{height:auto!important;padding:0!important;color:var(--text-2)!important;cursor:pointer;text-align:left;max-width:80%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.popular-link:hover{color:var(--text)!important}.panel-empty{height:150px;display:grid;place-items:center;color:var(--muted);font-size:.8rem}.task-summary-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:10px}.task-summary-card{border:1px solid var(--border);background:var(--surface-2);border-radius:7px;padding:14px;display:grid;gap:7px}.task-summary-card b{font-size:.82rem;margin-bottom:3px}.task-summary-card span{font-size:.75rem;color:var(--text-2)}.danger{color:#ef4444!important}@media(max-width:1000px){.task-summary-grid{grid-template-columns:1fr}}
</style>
