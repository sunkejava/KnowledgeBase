<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft, Download, FilePlus2, History, Paperclip, Save, Share2, Star, Tags, Trash2, Upload } from 'lucide-vue-next'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import type { TableColumn } from '../types/table'
import { documentApi } from '../api/modules/documents'

const route = useRoute()
const router = useRouter()
const knowledgeBaseId = String(route.params.id)
const knowledgeBase = ref<any>(null)
const docs = ref<any[]>([])
const selectedId = ref('')
const saving = ref(false)
const editor = reactive({ id: '', parentId: null as string | null, title: '', slug: '', markdown: '' })
const versions = ref<any[]>([])
const allTags = ref<any[]>([])
const tagIds = ref<string[]>([])
const attachments = ref<any[]>([])
const metaDrawer = ref('')
const favorite = ref(false)
const diffLines = ref<any[]>([])
const fileInput = ref<HTMLInputElement | null>(null)
const importInput = ref<HTMLInputElement | null>(null)
const supportedDocumentAccept = '.txt,.md,.markdown,.html,.htm,.pdf,.doc,.docx,.xls,.xlsx,.csv'

const versionColumns: TableColumn<any>[] = [
  { key: 'versionNumber', label: '版本', width: 90, formatter: row => `v${row.versionNumber}`, sortable: true },
  { key: 'title', label: '标题', minWidth: 180 },
  { key: 'changeNote', label: '说明', minWidth: 220, formatter: row => row.changeNote || '无说明' },
  { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt), sortable: true }
]
const attachmentColumns: TableColumn<any>[] = [
  { key: 'fileName', label: '文件名', minWidth: 220, sortable: true },
  { key: 'contentType', label: '类型', width: 160 },
  { key: 'size', label: '大小', width: 120, formatter: row => formatSize(row.size), sortable: true },
  { key: 'createdAt', label: '上传时间', width: 190, formatter: row => formatTime(row.createdAt), sortable: true }
]

const treeData = computed(() => {
  const map = new Map<string, any>()
  const roots: any[] = []
  docs.value.forEach(item => map.set(item.id, { ...item, label: item.title, children: [] }))
  map.forEach(node => {
    if (node.parentId && map.has(node.parentId)) map.get(node.parentId).children.push(node)
    else roots.push(node)
  })
  return roots
})

function formatTime(value: string) {
  return value ? new Date(value).toLocaleString() : '-'
}
function formatSize(value: number) {
  if (!value) return '0 KB'
  if (value < 1024 * 1024) return `${Math.ceil(value / 1024)} KB`
  return `${(value / 1024 / 1024).toFixed(2)} MB`
}

async function load() {
  const [base, documentList] = await Promise.all([
    documentApi.knowledgeBase(knowledgeBaseId),
    documentApi.list(knowledgeBaseId)
  ])
  knowledgeBase.value = base.data
  docs.value = documentList.data
}

async function loadMeta() {
  if (!editor.id) return
  const [versionData, selectedTags, tags, files, favorites] = await Promise.all([
    documentApi.versions(editor.id),
    documentApi.documentTags(editor.id),
    documentApi.tags(),
    documentApi.attachments(editor.id),
    documentApi.favorites()
  ])
  versions.value = versionData.data
  tagIds.value = selectedTags.data.map((item: any) => item.id)
  allTags.value = tags.data
  attachments.value = files.data
  favorite.value = favorites.data.some((item: any) => item.documentId === editor.id)
}

async function select(node: any) {
  selectedId.value = node.id
  const { data } = await documentApi.get(node.id)
  Object.assign(editor, { id: data.id, parentId: data.parentId, title: data.title, slug: data.slug, markdown: data.markdown })
  await documentApi.recent(node.id)
  await loadMeta()
  await router.replace({ query: { ...route.query, document: node.id } })
}

function createDoc() {
  selectedId.value = ''
  Object.assign(editor, { id: '', parentId: null, title: '未命名文档', slug: `doc-${Date.now()}`, markdown: '# 未命名文档\n\n开始记录知识内容。' })
  versions.value = []
  tagIds.value = []
  attachments.value = []
  favorite.value = false
}
function createChild() {
  Object.assign(editor, { id: '', parentId: editor.id || null, title: '新建子文档', slug: `doc-${Date.now()}`, markdown: '# 新建子文档\n' })
}

async function save() {
  if (!editor.title.trim()) return ElMessage.warning('请输入文档标题')
  saving.value = true
  try {
    if (editor.id) {
      await documentApi.createVersion(editor.id, '编辑前自动快照')
      editor.id = (await documentApi.update(editor.id, { parentId: editor.parentId, title: editor.title, slug: editor.slug, markdown: editor.markdown })).data.id
    } else {
      editor.id = (await documentApi.create({ knowledgeBaseId, parentId: editor.parentId, title: editor.title, slug: editor.slug, markdown: editor.markdown })).data.id
    }
    await load()
    selectedId.value = editor.id
    await loadMeta()
    await router.replace({ query: { ...route.query, document: editor.id } })
    ElMessage.success('文档已保存')
  } finally {
    saving.value = false
  }
}

async function remove() {
  if (!editor.id) return
  await ElMessageBox.confirm(`确认删除“${editor.title}”？`, '删除文档', { type: 'warning' })
  await documentApi.remove(editor.id)
  Object.assign(editor, { id: '', parentId: null, title: '', slug: '', markdown: '' })
  selectedId.value = ''
  await load()
}

async function toggleFavorite() {
  if (!editor.id) return
  favorite.value = (await documentApi.favorite(editor.id)).data.favorite
  ElMessage.success(favorite.value ? '已收藏' : '已取消收藏')
}
async function saveTags() {
  if (!editor.id) return
  await documentApi.saveDocumentTags(editor.id, tagIds.value)
  ElMessage.success('标签已更新')
}
async function createSnapshot() {
  if (!editor.id) return
  const note = prompt('版本说明', '手动快照') || '手动快照'
  await documentApi.createVersion(editor.id, note)
  await loadMeta()
  ElMessage.success('版本快照已创建')
}
async function restoreVersion(version: any) {
  await ElMessageBox.confirm(`确认恢复到 v${version.versionNumber}？当前版本会自动备份。`, '恢复版本', { type: 'warning' })
  await documentApi.restoreVersion(version.id)
  await select({ id: editor.id })
  ElMessage.success('版本已恢复')
}
async function compareVersion(version: any) {
  diffLines.value = (await documentApi.diffVersion(version.id)).data.lines
  metaDrawer.value = 'diff'
}

function chooseFile() {
  fileInput.value?.click()
}
async function uploadFile(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !editor.id) return
  const form = new FormData()
  form.append('file', file)
  await documentApi.uploadAttachment(editor.id, form)
  input.value = ''
  await loadMeta()
  ElMessage.success('附件已上传')
}
async function downloadAttachment(item: any) {
  const { data } = await documentApi.downloadAttachment(item.downloadUrl)
  downloadBlob(data, item.fileName)
}
async function deleteAttachment(item: any) {
  await ElMessageBox.confirm(`删除附件“${item.fileName}”？`, '删除附件', { type: 'warning' })
  await documentApi.deleteAttachment(item.id)
  await loadMeta()
}

async function exportMarkdown() {
  if (!editor.id) return
  const { data } = await documentApi.exportMarkdown(editor.id)
  downloadBlob(data, `${editor.title || 'document'}.md`)
}

/**
 * 在当前知识库中导入常见文档。
 * 如果当前已打开文档，则导入结果作为该文档的子文档；否则作为根文档。
 */
async function importDocument(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  const form = new FormData()
  form.append('file', file)
  const { data } = await documentApi.importDocument(knowledgeBaseId, form, editor.id || undefined)
  input.value = ''
  await load()
  await select({ id: data.documentId })
  ElMessage.success(`${file.name} 已导入`)
}
async function createShare() {
  if (!editor.id) return
  const { data } = await documentApi.createShare(editor.id)
  const url = `${location.origin}/share/${data.token}`
  await navigator.clipboard.writeText(url)
  ElMessage.success('分享链接已复制到剪贴板')
}
async function openMeta(name: string) {
  metaDrawer.value = name
  if (editor.id) await loadMeta()
}
function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}

onMounted(async () => {
  await load()
  const target = String(route.query.document || '')
  if (target && docs.value.some(item => item.id === target)) {
    await nextTick()
    await select({ id: target })
  }
})
</script>

<template>
  <section class="workspace-page">
    <header class="workspace-head">
      <button @click="router.push('/knowledge-bases')"><ArrowLeft :size="16"/>返回</button>
      <div><strong>{{ knowledgeBase?.name || '知识库' }}</strong><span>{{ knowledgeBase?.description }}</span></div>
      <div class="workspace-actions">
        <button @click="createDoc"><FilePlus2 :size="15"/>新建文档</button>
        <button :disabled="!editor.id" @click="createChild">新建子文档</button>
        <input ref="importInput" type="file" :accept="supportedDocumentAccept" hidden @change="importDocument"/>
        <button title="支持 TXT、Markdown、HTML、PDF、DOC/DOCX、XLS/XLSX、CSV" @click="importInput?.click()"><Upload :size="15"/>导入</button>
        <button :disabled="!editor.id" @click="exportMarkdown"><Download :size="15"/>导出</button>
        <button :disabled="!editor.id" @click="createShare"><Share2 :size="15"/>分享</button>
        <button :disabled="!editor.id" @click="toggleFavorite"><Star :size="15" :fill="favorite?'currentColor':'none'"/>{{favorite?'已收藏':'收藏'}}</button>
        <button :disabled="!editor.id" @click="openMeta('versions')"><History :size="15"/>版本</button>
        <button :disabled="!editor.id" @click="openMeta('tags')"><Tags :size="15"/>标签</button>
        <button :disabled="!editor.id" @click="openMeta('attachments')"><Paperclip :size="15"/>附件</button>
        <button class="primary" :disabled="!editor.title" @click="save"><Save :size="15"/>{{saving?'保存中':'保存'}}</button>
        <button class="danger-text" :disabled="!editor.id" @click="remove"><Trash2 :size="15"/>删除</button>
      </div>
    </header>

    <div class="workspace-body">
      <aside class="doc-tree">
        <div class="tree-title">文档目录 <span>{{docs.length}}</span></div>
        <el-tree :data="treeData" node-key="id" :current-node-key="selectedId" highlight-current default-expand-all @node-click="select"/>
        <div v-if="!docs.length" class="tree-empty">暂无文档</div>
      </aside>
      <main class="editor-pane">
        <div v-if="editor.title" class="editor-shell">
          <input v-model="editor.title" class="title-input"/>
          <div class="meta-line"><span>Slug</span><input v-model="editor.slug"/></div>
          <div class="editor-grid"><textarea v-model="editor.markdown" spellcheck="false"/><div class="preview"><div class="preview-label">PREVIEW</div><pre>{{editor.markdown}}</pre></div></div>
        </div>
        <div v-else class="editor-empty"><strong>选择或创建一篇文档</strong><p>正文仅在打开文档时加载，不随目录一次性返回。</p><button class="primary" @click="createDoc">创建第一篇文档</button></div>
      </main>
    </div>

    <el-drawer v-model="metaDrawer" :title="metaDrawer==='versions'?'版本历史':metaDrawer==='tags'?'文档标签':metaDrawer==='attachments'?'附件':'版本差异'" size="620px" @close="metaDrawer=''">
      <BaseDataTable v-if="metaDrawer==='versions'" :rows="versions" :columns="versionColumns" storage-key="workspace-versions" export-file-name="文档版本" :show-refresh="false" :action-width="140">
        <template #toolbar><el-button type="primary" @click="createSnapshot">创建快照</el-button></template>
        <template #actions="{ row }"><el-button link type="primary" @click="compareVersion(row)">对比</el-button><el-button link type="primary" @click="restoreVersion(row)">恢复</el-button></template>
      </BaseDataTable>

      <div v-if="metaDrawer==='tags'">
        <el-select v-model="tagIds" multiple filterable style="width:100%" placeholder="选择标签"><el-option v-for="item in allTags" :key="item.id" :label="item.name" :value="item.id"/></el-select>
        <el-button type="primary" style="margin-top:14px" @click="saveTags">保存标签</el-button>
      </div>

      <BaseDataTable v-if="metaDrawer==='attachments'" :rows="attachments" :columns="attachmentColumns" storage-key="workspace-attachments" export-file-name="文档附件" :show-refresh="false" :action-width="140">
        <template #toolbar><input ref="fileInput" type="file" hidden @change="uploadFile"/><el-button type="primary" @click="chooseFile">上传附件</el-button></template>
        <template #actions="{ row }"><el-button link type="primary" @click="downloadAttachment(row)">下载</el-button><el-button link type="danger" @click="deleteAttachment(row)">删除</el-button></template>
      </BaseDataTable>

      <div v-if="metaDrawer==='diff'" class="diff-box"><div v-for="(line,index) in diffLines" :key="index" :class="['diff-line','diff-'+line.type]"><span>{{line.oldLine||''}}</span><span>{{line.newLine||''}}</span><code>{{line.type==='add'?'+ ':line.type==='delete'?'- ':'  '}}{{line.text}}</code></div></div>
    </el-drawer>
  </section>
</template>

<style scoped>
.workspace-page{height:calc(100vh - 58px);display:flex;flex-direction:column}.workspace-head{min-height:72px;border-bottom:1px solid var(--border);display:flex;align-items:center;padding:10px 22px;gap:18px}.workspace-head>button,.workspace-actions button{height:32px;border:1px solid var(--border);background:var(--surface);color:var(--text);border-radius:6px;display:flex;align-items:center;gap:6px;padding:0 10px}.workspace-head>div:nth-child(2){min-width:0}.workspace-head strong{display:block;font-size:14px}.workspace-head span{display:block;font-size:10px;color:var(--muted);margin-top:4px}.workspace-actions{margin-left:auto;display:flex;gap:7px;flex-wrap:wrap;justify-content:flex-end}.workspace-body{min-height:0;flex:1;display:grid;grid-template-columns:260px 1fr}.doc-tree{border-right:1px solid var(--border);padding:14px;background:var(--sidebar);overflow:auto}.tree-title{font-size:11px;color:var(--muted);padding:4px 8px 12px}.tree-title span{float:right}.tree-empty{font-size:11px;color:var(--muted);padding:20px 8px}.editor-pane{min-width:0;overflow:auto;background:var(--bg)}.editor-shell{height:100%;display:flex;flex-direction:column;padding:20px 24px}.title-input{font-size:25px;font-weight:650;background:transparent;border:0;color:var(--text);outline:none;padding:8px 0}.meta-line{display:flex;align-items:center;gap:10px;font-size:10px;color:var(--muted);margin:3px 0 14px}.meta-line input{width:260px;background:var(--surface);border:1px solid var(--border);color:var(--text);border-radius:5px;height:27px;padding:0 8px}.editor-grid{min-height:0;flex:1;display:grid;grid-template-columns:1fr 1fr;border:1px solid var(--border);border-radius:8px;overflow:hidden}.editor-grid textarea{resize:none;border:0;border-right:1px solid var(--border);outline:none;background:var(--surface);color:var(--text);padding:18px;font:13px/1.8 Consolas,monospace}.preview{padding:18px;overflow:auto;background:var(--surface-2)}.preview-label{font-size:9px;letter-spacing:.15em;color:var(--muted);margin-bottom:12px}.preview pre{white-space:pre-wrap;font:13px/1.8 inherit;color:var(--text)}.editor-empty{height:100%;display:grid;place-items:center;align-content:center;text-align:center}.editor-empty strong{font-size:17px}.editor-empty p{color:var(--muted);font-size:12px}.editor-empty button{height:34px;border-radius:6px;padding:0 13px}.danger-text{color:#e56b6b!important}.diff-box{font:12px/1.6 Consolas,monospace;border:1px solid var(--border);border-radius:7px;overflow:auto}.diff-line{display:grid;grid-template-columns:38px 38px 1fr;min-height:25px}.diff-line>span{color:var(--muted);text-align:right;padding:3px 7px;border-right:1px solid var(--border)}.diff-line code{white-space:pre-wrap;padding:3px 8px;color:var(--text)}.diff-add{background:rgba(34,197,94,.09)}.diff-delete{background:rgba(239,68,68,.09)}@media(max-width:1100px){.workspace-body{grid-template-columns:220px 1fr}}@media(max-width:900px){.workspace-body{grid-template-columns:190px 1fr}.editor-grid{grid-template-columns:1fr}.preview{display:none}}
</style>