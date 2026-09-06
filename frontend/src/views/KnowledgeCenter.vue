<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import type { TableColumn } from '../types/table'
import { knowledgeApi } from '../api/modules/knowledge'

const route = useRoute()
const router = useRouter()
const tab = ref(String(route.query.tab || 'search'))
const keyword = ref('')
const rows = ref<any[]>([])
const loading = ref(false)

const columns = computed<TableColumn<any>[]>(() => {
  if (tab.value === 'search') return [
    { key: 'title', label: '文档', minWidth: 240, sortable: true },
    { key: 'snippet', label: '内容摘要', minWidth: 520 },
    { key: 'updatedAt', label: '更新时间', width: 190, formatter: row => formatTime(row.updatedAt) },
    { key: 'knowledgeBaseId', label: '知识库 ID', width: 280, visible: false }
  ]
  if (tab.value === 'recent') return [
    { key: 'title', label: '文档', minWidth: 300, sortable: true },
    { key: 'viewCount', label: '访问次数', width: 120, sortable: true },
    { key: 'lastViewedAt', label: '最近访问', width: 190, formatter: row => formatTime(row.lastViewedAt), sortable: true },
    { key: 'documentId', label: '文档 ID', width: 280, visible: false }
  ]
  if (tab.value === 'favorites') return [
    { key: 'title', label: '文档', minWidth: 320, sortable: true },
    { key: 'createdAt', label: '收藏时间', width: 190, formatter: row => formatTime(row.createdAt), sortable: true },
    { key: 'documentId', label: '文档 ID', width: 280, visible: false }
  ]
  return [
    { key: 'name', label: '标签', minWidth: 260, sortable: true },
    { key: 'color', label: '颜色值', width: 160 },
    { key: 'id', label: '标签 ID', minWidth: 280, visible: false }
  ]
})

function formatTime(value: string) {
  return value ? new Date(value).toLocaleString() : '-'
}

async function load() {
  loading.value = true
  try {
    if (tab.value === 'recent') rows.value = (await knowledgeApi.recent()).data
    else if (tab.value === 'favorites') rows.value = (await knowledgeApi.favorites()).data
    else if (tab.value === 'tags') rows.value = (await knowledgeApi.tags()).data
    else if (keyword.value.trim()) rows.value = (await knowledgeApi.search(keyword.value.trim())).data
    else rows.value = []
  } finally {
    loading.value = false
  }
}

async function open(row: any) {
  if (tab.value === 'tags') return
  const documentId = row.documentId || row.id
  if (!documentId) return
  let knowledgeBaseId = row.knowledgeBaseId
  if (!knowledgeBaseId) knowledgeBaseId = (await knowledgeApi.document(documentId)).data.knowledgeBaseId
  if (knowledgeBaseId) await router.push({ path: `/knowledge-bases/${knowledgeBaseId}`, query: { document: documentId } })
}

watch(tab, async value => {
  await router.replace({ query: { ...route.query, tab: value } })
  keyword.value = ''
  await load()
})

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>知识中心</h1><p>统一检索、收藏、最近访问与标签资产；双击文档可直接定位到工作区。</p></div></div>
    <el-tabs v-model="tab">
      <el-tab-pane label="全文搜索" name="search"/><el-tab-pane label="最近浏览" name="recent"/>
      <el-tab-pane label="我的收藏" name="favorites"/><el-tab-pane label="标签" name="tags"/>
    </el-tabs>

    <BaseDataTable
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :storage-key="`knowledge-center-${tab}`"
      :export-file-name="`KnowledgeBase-${tab}`"
      @refresh="load"
      @row-dblclick="open"
    >
      <template #toolbar>
        <template v-if="tab==='search'">
          <el-input v-model="keyword" clearable placeholder="搜索标题或 Markdown 正文" style="width:360px" @keyup.enter="load" />
          <el-button type="primary" @click="load">搜索</el-button>
        </template>
        <span v-else class="list-hint">双击文档行可直接打开对应知识库工作区</span>
      </template>
      <template #cell-color="{ value }"><span class="tag-color"><i :style="{background:value}" />{{ value }}</span></template>
    </BaseDataTable>
  </section>
</template>

<style scoped>
.list-hint{font-size:12px;color:var(--muted)}.tag-color{display:inline-flex;align-items:center;gap:8px}.tag-color i{width:10px;height:10px;border-radius:50%;border:1px solid var(--border)}
</style>
