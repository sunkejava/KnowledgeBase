<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import PageHeader from '../components/common/PageHeader.vue'
import type { TableColumn } from '../types/table'
import { collaborationApi } from '../api/modules/collaboration'

const router = useRouter()
const rows = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const loading = ref(false)
const keyword = ref('')
const unreadCount = ref(0)

const columns: TableColumn<any>[] = [
  { key: 'isRead', label: '状态', width: 90, formatter: row => row.isRead ? '已读' : '未读' },
  { key: 'type', label: '类型', width: 110 },
  { key: 'title', label: '标题', minWidth: 220 },
  { key: 'content', label: '内容', minWidth: 360 },
  { key: 'createdAt', label: '时间', width: 190, formatter: row => new Date(row.createdAt).toLocaleString() }
]

async function load() {
  loading.value = true
  try {
    const [{ data }, summary] = await Promise.all([
      collaborationApi.notifications(page.value, pageSize.value, keyword.value),
      collaborationApi.notificationSummary()
    ])
    rows.value = data.items
    total.value = data.total
    unreadCount.value = summary.data.unreadCount
  } finally {
    loading.value = false
  }
}

async function pageChange(nextPage: number, nextSize: number) {
  page.value = nextPage
  pageSize.value = nextSize
  await load()
}

async function search() {
  page.value = 1
  await load()
}

async function markRead(row: any) {
  if (!row.isRead) await collaborationApi.markRead(row.id)
  if (row.targetUrl) await router.push(row.targetUrl)
  await load()
}

async function markAllRead() {
  const { data } = await collaborationApi.markAllRead()
  ElMessage.success(`已标记 ${data.updatedCount} 条通知为已读`)
  await load()
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader title="通知中心" :description="`统一承载评论 @成员、系统提醒和后续任务通知。当前未读 ${unreadCount} 条。`">
      <template #actions>
        <el-input v-model="keyword" clearable placeholder="搜索标题或内容" style="width:260px" @keyup.enter="search"/>
        <el-button @click="search">查询</el-button>
        <el-button type="primary" @click="markAllRead">全部已读</el-button>
      </template>
    </PageHeader>

    <BaseDataTable
      :rows="rows"
      :columns="columns"
      :loading="loading"
      storage-key="notifications"
      export-file-name="KnowledgeBase-通知"
      server-paging
      :total-count="total"
      :action-width="100"
      @refresh="load"
      @page-change="pageChange"
    >
      <template #cell-isRead="{ row }"><el-tag :type="row.isRead ? 'info' : 'danger'">{{ row.isRead ? '已读' : '未读' }}</el-tag></template>
      <template #actions="{ row }"><el-button link type="primary" @click="markRead(row)">{{ row.targetUrl ? '查看' : '已读' }}</el-button></template>
    </BaseDataTable>
  </section>
</template>
