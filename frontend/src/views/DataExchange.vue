<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import PageHeader from '../components/common/PageHeader.vue'
import type { TableColumn } from '../types/table'
import { knowledgeApi } from '../api/modules/knowledge'
import { exchangeApi } from '../api/modules/exchange'

const tab = ref('export')
const knowledgeBases = ref<any[]>([])
const selectedKnowledgeBaseId = ref('')
const exportTasks = ref<any[]>([])
const importTasks = ref<any[]>([])
const exportTotal = ref(0)
const importTotal = ref(0)
const exportPage = ref(1)
const importPage = ref(1)
const exportPageSize = ref(20)
const importPageSize = ref(20)
const exportLoading = ref(false)
const importLoading = ref(false)
const zipInput = ref<HTMLInputElement | null>(null)
const htmlInput = ref<HTMLInputElement | null>(null)

const exportColumns: TableColumn<any>[] = [
  { key: 'name', label: '任务名称', minWidth: 260 },
  { key: 'format', label: '格式', width: 90 },
  { key: 'status', label: '状态', width: 120 },
  { key: 'fileName', label: '文件', minWidth: 220, formatter: row => row.fileName || '-' },
  { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt) },
  { key: 'completedAt', label: '完成时间', width: 190, formatter: row => formatTime(row.completedAt) },
  { key: 'errorMessage', label: '失败原因', minWidth: 220, formatter: row => row.errorMessage || '-' }
]

const importColumns: TableColumn<any>[] = [
  { key: 'name', label: '任务名称', minWidth: 260 },
  { key: 'status', label: '状态', width: 120 },
  { key: 'progress', label: '进度', width: 150, formatter: row => `${row.processedCount}/${row.totalCount || '-'}` },
  { key: 'importedCount', label: '成功', width: 90 },
  { key: 'skippedCount', label: '跳过', width: 90 },
  { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt) },
  { key: 'completedAt', label: '完成时间', width: 190, formatter: row => formatTime(row.completedAt) },
  { key: 'errorMessage', label: '失败原因', minWidth: 220, formatter: row => row.errorMessage || '-' }
]

function formatTime(value?: string) { return value ? new Date(value).toLocaleString() : '-' }
function statusText(value: string) {
  return ({ Pending: '排队中', Running: '处理中', Completed: '已完成', Failed: '失败', Cancelled: '已取消' } as Record<string, string>)[value] || value
}
function canCancel(row: any) { return row.status === 'Pending' }
function canRetry(row: any) { return row.status === 'Failed' || row.status === 'Cancelled' }

async function loadBaseOptions() {
  const { data } = await knowledgeApi.knowledgeBases(1, 100, '')
  knowledgeBases.value = data.items
  if (!selectedKnowledgeBaseId.value && data.items.length) selectedKnowledgeBaseId.value = data.items[0].id
}
async function loadExportTasks() {
  exportLoading.value = true
  try {
    const { data } = await exchangeApi.exportTasks(exportPage.value, exportPageSize.value)
    exportTasks.value = data.items
    exportTotal.value = data.total
  } finally { exportLoading.value = false }
}
async function loadImportTasks() {
  importLoading.value = true
  try {
    const { data } = await exchangeApi.importTasks(importPage.value, importPageSize.value)
    importTasks.value = data.items
    importTotal.value = data.total
  } finally { importLoading.value = false }
}
async function createExport() {
  if (!selectedKnowledgeBaseId.value) return ElMessage.warning('请选择知识库')
  await exchangeApi.createExportTask(selectedKnowledgeBaseId.value)
  ElMessage.success('导出任务已进入队列')
  exportPage.value = 1
  await loadExportTasks()
}
async function exportPageChange(nextPage: number, nextSize: number) {
  exportPage.value = nextPage
  exportPageSize.value = nextSize
  await loadExportTasks()
}
async function importPageChange(nextPage: number, nextSize: number) {
  importPage.value = nextPage
  importPageSize.value = nextSize
  await loadImportTasks()
}
async function download(row: any) {
  const { data } = await exchangeApi.downloadExportTask(row.id)
  const url = URL.createObjectURL(data)
  const link = document.createElement('a')
  link.href = url
  link.download = row.fileName || 'knowledge-base.zip'
  link.click()
  URL.revokeObjectURL(url)
}
async function cancelExport(row: any) { await exchangeApi.cancelExportTask(row.id); ElMessage.success('导出任务已取消'); await loadExportTasks() }
async function retryExport(row: any) { await exchangeApi.retryExportTask(row.id); ElMessage.success('导出任务已重新排队'); await loadExportTasks() }
async function cancelImport(row: any) { await exchangeApi.cancelImportTask(row.id); ElMessage.success('导入任务已取消'); await loadImportTasks() }
async function retryImport(row: any) { await exchangeApi.retryImportTask(row.id); ElMessage.success('导入任务已重新排队'); await loadImportTasks() }
async function cleanup(kind: 'export' | 'import') {
  await ElMessageBox.confirm('确认清理 7 天前已经结束的任务记录和相关临时文件？', '清理任务', { type: 'warning' })
  const { data } = kind === 'export' ? await exchangeApi.cleanupExportTasks(7) : await exchangeApi.cleanupImportTasks(7)
  ElMessage.success(`已清理 ${data.deletedCount} 条任务`)
  if (kind === 'export') await loadExportTasks(); else await loadImportTasks()
}
async function importZip(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !selectedKnowledgeBaseId.value) return
  const form = new FormData(); form.append('file', file)
  await exchangeApi.createImportTask(selectedKnowledgeBaseId.value, form)
  input.value = ''
  tab.value = 'import'
  importPage.value = 1
  ElMessage.success('ZIP 已上传，导入任务进入队列')
  await loadImportTasks()
}
async function importHtml(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !selectedKnowledgeBaseId.value) return
  const form = new FormData(); form.append('file', file)
  await exchangeApi.importHtml(selectedKnowledgeBaseId.value, form)
  input.value = ''
  ElMessage.success('HTML 已转换为 Markdown 并导入')
}

onMounted(async () => { await Promise.all([loadBaseOptions(), loadExportTasks(), loadImportTasks()]) })
</script>

<template>
  <section class="page">
    <PageHeader title="数据交换" description="知识库导出与批量导入统一通过任务中心管理；ZIP 会保留并恢复文档父子目录层级。">
      <template #actions>
        <el-select v-model="selectedKnowledgeBaseId" filterable placeholder="选择知识库" style="width:260px"><el-option v-for="item in knowledgeBases" :key="item.id" :label="item.name" :value="item.id"/></el-select>
        <el-button type="primary" @click="createExport">创建 ZIP 导出任务</el-button>
        <input ref="zipInput" type="file" accept=".zip" hidden @change="importZip"/>
        <el-button @click="zipInput?.click()">异步导入 Markdown ZIP</el-button>
        <input ref="htmlInput" type="file" accept=".html,.htm" hidden @change="importHtml"/>
        <el-button @click="htmlInput?.click()">导入 HTML</el-button>
      </template>
    </PageHeader>

    <el-tabs v-model="tab">
      <el-tab-pane label="导出任务" name="export">
        <BaseDataTable :rows="exportTasks" :columns="exportColumns" :loading="exportLoading" storage-key="export-tasks" export-file-name="KnowledgeBase-导出任务" server-paging :total-count="exportTotal" :action-width="210" @refresh="loadExportTasks" @page-change="exportPageChange">
          <template #toolbar><el-button @click="cleanup('export')">清理 7 天前任务</el-button></template>
          <template #cell-status="{ value }"><el-tag :type="value==='Completed'?'success':value==='Failed'?'danger':value==='Running'?'warning':'info'">{{ statusText(value) }}</el-tag></template>
          <template #actions="{ row }">
            <el-button link type="primary" :disabled="row.status!=='Completed'" @click="download(row)">下载</el-button>
            <el-button link :disabled="!canCancel(row)" @click="cancelExport(row)">取消</el-button>
            <el-button link type="warning" :disabled="!canRetry(row)" @click="retryExport(row)">重试</el-button>
          </template>
        </BaseDataTable>
      </el-tab-pane>

      <el-tab-pane label="导入任务" name="import">
        <BaseDataTable :rows="importTasks" :columns="importColumns" :loading="importLoading" storage-key="import-tasks" export-file-name="KnowledgeBase-导入任务" server-paging :total-count="importTotal" :action-width="150" @refresh="loadImportTasks" @page-change="importPageChange">
          <template #toolbar><el-button @click="cleanup('import')">清理 7 天前任务</el-button></template>
          <template #cell-status="{ value }"><el-tag :type="value==='Completed'?'success':value==='Failed'?'danger':value==='Running'?'warning':'info'">{{ statusText(value) }}</el-tag></template>
          <template #cell-progress="{ row }"><el-progress :percentage="row.totalCount ? Math.min(100, Math.round(row.processedCount * 100 / row.totalCount)) : 0" :stroke-width="8"/></template>
          <template #actions="{ row }">
            <el-button link :disabled="!canCancel(row)" @click="cancelImport(row)">取消</el-button>
            <el-button link type="warning" :disabled="!canRetry(row)" @click="retryImport(row)">重试</el-button>
          </template>
        </BaseDataTable>
      </el-tab-pane>
    </el-tabs>
  </section>
</template>
