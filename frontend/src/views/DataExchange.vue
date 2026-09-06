<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import PageHeader from '../components/common/PageHeader.vue'
import type { TableColumn } from '../types/table'
import { knowledgeApi } from '../api/modules/knowledge'
import { exchangeApi } from '../api/modules/exchange'

const knowledgeBases = ref<any[]>([])
const selectedKnowledgeBaseId = ref('')
const tasks = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const loading = ref(false)
const zipInput = ref<HTMLInputElement | null>(null)

const columns: TableColumn<any>[] = [
  { key: 'name', label: '任务名称', minWidth: 260 },
  { key: 'format', label: '格式', width: 90 },
  { key: 'status', label: '状态', width: 120 },
  { key: 'fileName', label: '文件', minWidth: 220, formatter: row => row.fileName || '-' },
  { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt) },
  { key: 'completedAt', label: '完成时间', width: 190, formatter: row => formatTime(row.completedAt) },
  { key: 'errorMessage', label: '失败原因', minWidth: 220, formatter: row => row.errorMessage || '-' }
]

function formatTime(value?: string) { return value ? new Date(value).toLocaleString() : '-' }
function statusText(value: string) { return ({ Pending: '排队中', Running: '处理中', Completed: '已完成', Failed: '失败' } as Record<string,string>)[value] || value }

async function loadBaseOptions() {
  const { data } = await knowledgeApi.knowledgeBases(1, 100, '')
  knowledgeBases.value = data.items
  if (!selectedKnowledgeBaseId.value && data.items.length) selectedKnowledgeBaseId.value = data.items[0].id
}
async function loadTasks() {
  loading.value = true
  try { const { data } = await exchangeApi.tasks(page.value, pageSize.value); tasks.value = data.items; total.value = data.total }
  finally { loading.value = false }
}
async function createExport() {
  if (!selectedKnowledgeBaseId.value) return ElMessage.warning('请选择知识库')
  await exchangeApi.createTask(selectedKnowledgeBaseId.value)
  ElMessage.success('导出任务已进入队列')
  page.value = 1
  await loadTasks()
}
async function pageChange(nextPage: number, nextSize: number) { page.value = nextPage; pageSize.value = nextSize; await loadTasks() }
async function download(row: any) {
  if (row.status !== 'Completed') return
  const { data } = await exchangeApi.downloadTask(row.id)
  const url = URL.createObjectURL(data)
  const link = document.createElement('a'); link.href = url; link.download = row.fileName || 'knowledge-base.zip'; link.click(); URL.revokeObjectURL(url)
}
async function importZip(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !selectedKnowledgeBaseId.value) return
  const form = new FormData(); form.append('file', file)
  const { data } = await exchangeApi.importZip(selectedKnowledgeBaseId.value, form)
  input.value = ''
  ElMessage.success(`导入完成：成功 ${data.importedCount}，跳过 ${data.skippedCount}`)
}

onMounted(async () => { await Promise.all([loadBaseOptions(), loadTasks()]) })
</script>

<template>
  <section class="page">
    <PageHeader title="数据交换" description="服务端异步导出知识库 Markdown ZIP，并支持批量 ZIP 导入。">
      <template #actions>
        <el-select v-model="selectedKnowledgeBaseId" filterable placeholder="选择知识库" style="width:260px"><el-option v-for="item in knowledgeBases" :key="item.id" :label="item.name" :value="item.id"/></el-select>
        <el-button type="primary" @click="createExport">创建 ZIP 导出任务</el-button>
        <input ref="zipInput" type="file" accept=".zip" hidden @change="importZip"/>
        <el-button @click="zipInput?.click()">导入 Markdown ZIP</el-button>
      </template>
    </PageHeader>
    <BaseDataTable :rows="tasks" :columns="columns" :loading="loading" storage-key="export-tasks" export-file-name="KnowledgeBase-导出任务" server-paging :total-count="total" :action-width="120" @refresh="loadTasks" @page-change="pageChange">
      <template #cell-status="{ value }"><el-tag :type="value==='Completed'?'success':value==='Failed'?'danger':value==='Running'?'warning':'info'">{{ statusText(value) }}</el-tag></template>
      <template #actions="{ row }"><el-button link type="primary" :disabled="row.status!=='Completed'" @click="download(row)">下载</el-button></template>
    </BaseDataTable>
  </section>
</template>
