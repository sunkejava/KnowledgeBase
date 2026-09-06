<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import PageHeader from '../components/common/PageHeader.vue'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import type { TableColumn } from '../types/table'
import { searchApi } from '../api/modules/search'

const status = ref<any>(null)
const tasks = ref<any[]>([])
const loading = ref(false)
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

const columns: TableColumn<any>[] = [
  { key: 'provider', label: '搜索引擎', width: 130 },
  { key: 'status', label: '状态', width: 120 },
  { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt) },
  { key: 'startedAt', label: '开始时间', width: 190, formatter: row => formatTime(row.startedAt) },
  { key: 'completedAt', label: '完成时间', width: 190, formatter: row => formatTime(row.completedAt) },
  { key: 'errorMessage', label: '失败原因', minWidth: 260, formatter: row => row.errorMessage || '-' }
]

function formatTime(value?: string) { return value ? new Date(value).toLocaleString() : '-' }
function statusText(value: string) { return ({ Pending: '排队中', Running: '处理中', Completed: '已完成', Failed: '失败' } as Record<string,string>)[value] || value }

async function loadStatus() {
  const { data } = await searchApi.status()
  status.value = data
}

async function loadTasks() {
  loading.value = true
  try {
    const { data } = await searchApi.tasks(page.value, pageSize.value)
    tasks.value = data.items
    total.value = data.total
  } finally {
    loading.value = false
  }
}

async function rebuild() {
  try {
    await searchApi.rebuild()
    ElMessage.success('索引重建任务已进入队列')
    page.value = 1
    await loadTasks()
  } catch (error: any) {
    ElMessage.error(error?.response?.data?.message || '创建索引任务失败')
  }
}

async function retry(row: any) {
  await searchApi.retry(row.id)
  ElMessage.success('任务已重新进入队列')
  await loadTasks()
}

async function cleanup() {
  await ElMessageBox.confirm('将清理 30 天前已完成或失败的索引任务历史，是否继续？', '清理任务历史', { type: 'warning' })
  const { data } = await searchApi.cleanup(30)
  ElMessage.success(`已清理 ${data.deleted} 条任务记录`)
  await loadTasks()
}

async function pageChange(nextPage: number, nextSize: number) {
  page.value = nextPage
  pageSize.value = nextSize
  await loadTasks()
}

onMounted(async () => { await Promise.all([loadStatus(), loadTasks()]) })
</script>

<template>
  <section class="page">
    <PageHeader title="搜索管理" description="查看当前搜索引擎、重建全文索引并维护索引任务历史。">
      <template #actions>
        <el-button @click="cleanup">清理历史</el-button>
        <el-button type="primary" @click="rebuild">重建全文索引</el-button>
      </template>
    </PageHeader>

    <div class="provider-card">
      <div>
        <span class="label">当前 Provider</span>
        <strong>{{ status?.provider || '-' }}</strong>
      </div>
      <div>
        <span class="label">运行模式</span>
        <strong>{{ status?.external ? '外部搜索服务' : '内置 SQLite' }}</strong>
      </div>
      <div>
        <span class="label">服务地址</span>
        <strong>{{ status?.endpoint || '无需外部服务' }}</strong>
      </div>
    </div>

    <el-alert v-if="status?.provider === 'sqlite'" type="info" :closable="false" show-icon style="margin-bottom:16px">
      SQLite 模式直接读取业务表，不维护独立索引。执行“重建全文索引”会立即完成；切换到 Meilisearch 后将执行真实全量索引重建。
    </el-alert>

    <BaseDataTable
      :rows="tasks"
      :columns="columns"
      :loading="loading"
      storage-key="search-index-tasks"
      export-file-name="KnowledgeBase-搜索索引任务"
      server-paging
      :total-count="total"
      :action-width="110"
      @refresh="loadTasks"
      @page-change="pageChange"
    >
      <template #cell-status="{ value }">
        <el-tag :type="value === 'Completed' ? 'success' : value === 'Failed' ? 'danger' : value === 'Running' ? 'warning' : 'info'">
          {{ statusText(value) }}
        </el-tag>
      </template>
      <template #actions="{ row }">
        <el-button link type="primary" :disabled="row.status !== 'Failed'" @click="retry(row)">重试</el-button>
      </template>
    </BaseDataTable>
  </section>
</template>

<style scoped>
.provider-card{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;margin-bottom:16px}.provider-card>div{border:1px solid var(--border);background:var(--surface);padding:18px;border-radius:8px}.label{display:block;font-size:11px;color:var(--muted);margin-bottom:8px}.provider-card strong{font-size:14px;word-break:break-all}@media(max-width:900px){.provider-card{grid-template-columns:1fr}}
</style>
