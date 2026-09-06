<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import PageHeader from '../components/common/PageHeader.vue'
import type { TableColumn } from '../types/table'
import { knowledgeApi } from '../api/modules/knowledge'

const route = useRoute()
const router = useRouter()
const tab = ref(String(route.query.tab || 'search'))
const keyword = ref('')
const rows = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
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

const isServerPaging = computed(() => tab.value !== 'tags')

function formatTime(value: string) {
  return value ? new Date(value).toLocaleString() : '-'
}

async function load() {
  loading.value = true
  try {
    if (tab.value === 'tags') {
      rows.value = (await knowledgeApi.tags()).data
      total.value = rows.value.length
      return
    }

    let response
    if (tab.value === 'recent') response = await knowledgeApi.recent(page.value, pageSize.value, keyword.value.trim())
    else if (tab.value === 'favorites') response = await knowledgeApi.favorites(page.value, pageSize.value, keyword.value.trim())
    else if (keyword.value.trim()) response = await knowledgeApi.search(keyword.value.trim(), page.value, pageSize.value)
    else {
      rows.value = []
      total.value = 0
      return
    }

    rows.value = response.data.items
    total.value = response.data.total
  } finally {
    loading.value = false
  }
}

async function search() {
  page.value = 1
  await load()
}

async function onPageChange(nextPage: number, nextPageSize: number) {
  page.value = nextPage
  pageSize.value = nextPageSize
  await load()
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
  page.value = 1
  await load()
})

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader title="知识中心" description="统一检索、收藏、最近访问与标签资产；搜索结果会自动按当前用户资源权限过滤。" />
    <el-tabs v-model="tab">
      <el-tab-pane label="全文搜索" name="search"/>
      <el-tab-pane label="最近浏览" name="recent"/>
      <el-tab-pane label="我的收藏" name="favorites"/>
      <el-tab-pane label="标签" name="tags"/>
    </el-tabs>

    <BaseDataTable
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :storage-key="`knowledge-center-${tab}`"
      :export-file-name="`KnowledgeBase-${tab}`"
      :server-paging="isServerPaging"
      :total-count="total"
      @refresh="load"
      @page-change="onPageChange"
      @row-dblclick="open"
    >
      <template #toolbar>
        <template v-if="tab!=='tags'">
          <el-input
            v-model="keyword"
            clearable
            :placeholder="tab==='search' ? '搜索标题或 Markdown 正文' : '按文档标题筛选'"
            style="width:360px"
            @keyup.enter="search"
          />
          <el-button type="primary" @click="search">查询</el-button>
        </template>
        <span v-else class="list-hint">标签数据量通常较小，当前采用本地分页。</span>
      </template>
      <template #cell-color="{ value }"><span class="tag-color"><i :style="{background:value}" />{{ value }}</span></template>
    </BaseDataTable>
  </section>
</template>

<style scoped>
.list-hint{font-size:12px;color:var(--muted)}.tag-color{display:inline-flex;align-items:center;gap:8px}.tag-color i{width:10px;height:10px;border-radius:50%;border:1px solid var(--border)}
</style>
