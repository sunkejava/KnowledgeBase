<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import PageHeader from '../components/common/PageHeader.vue'
import type { TableColumn } from '../types/table'
import { knowledgeApi } from '../api/modules/knowledge'

interface Item { id:string; name:string; description:string; createdAt:string; updatedAt:string }

const router = useRouter()
const items = ref<Item[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const loading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const keyword = ref('')
const form = reactive({ name: '', description: '' })

const columns: TableColumn<Item>[] = [
  { key: 'name', label: '知识库名称', minWidth: 220, sortable: true },
  { key: 'description', label: '说明', minWidth: 360, formatter: row => row.description || '-' },
  { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt), sortable: true },
  { key: 'updatedAt', label: '更新时间', width: 190, formatter: row => formatTime(row.updatedAt), sortable: true },
  { key: 'id', label: '知识库 ID', width: 280, visible: false }
]

function formatTime(value: string) {
  return value ? new Date(value).toLocaleString() : '-'
}

async function load() {
  loading.value = true
  try {
    const { data } = await knowledgeApi.knowledgeBases(page.value, pageSize.value, keyword.value.trim())
    items.value = data.items
    total.value = data.total
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

function openCreate() {
  editingId.value = null
  form.name = ''
  form.description = ''
  dialogVisible.value = true
}
function openEdit(item: Item) {
  editingId.value = item.id
  form.name = item.name
  form.description = item.description
  dialogVisible.value = true
}
async function save() {
  if (!form.name.trim()) return ElMessage.warning('请输入知识库名称')
  if (editingId.value) await knowledgeApi.updateKnowledgeBase(editingId.value, form)
  else await knowledgeApi.createKnowledgeBase(form)
  dialogVisible.value = false
  ElMessage.success('保存成功')
  await load()
}
async function remove(item: Item) {
  await ElMessageBox.confirm(`确认删除知识库“${item.name}”？`, '删除确认', { type: 'warning' })
  await knowledgeApi.deleteKnowledgeBase(item.id)
  ElMessage.success('已删除')
  await load()
}
function open(item: Item) {
  router.push(`/knowledge-bases/${item.id}`)
}
function openAccess(item: Item) {
  router.push(`/knowledge-bases/${item.id}/access`)
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader title="我的知识库" description="仅展示当前账号有权访问的知识库；创建人自动拥有 Manager 权限。">
      <template #actions><el-button type="primary" @click="openCreate">创建知识库</el-button></template>
    </PageHeader>

    <BaseDataTable
      :rows="items"
      :columns="columns"
      :loading="loading"
      storage-key="knowledge-bases"
      export-file-name="KnowledgeBase-知识库列表"
      :action-width="270"
      server-paging
      :total-count="total"
      @refresh="load"
      @page-change="onPageChange"
      @row-dblclick="open"
    >
      <template #toolbar>
        <el-input v-model="keyword" clearable placeholder="搜索知识库名称或说明" style="width:320px" @keyup.enter="search" />
        <el-button type="primary" @click="search">查询</el-button>
      </template>
      <template #actions="{ row }">
        <el-button link type="primary" @click="open(row)">进入</el-button>
        <el-button link type="primary" @click="openAccess(row)">权限</el-button>
        <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
        <el-button link type="danger" @click="remove(row)">删除</el-button>
      </template>
      <template #empty>当前账号暂无可访问的知识库。</template>
    </BaseDataTable>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑知识库' : '创建知识库'" width="520px">
      <el-form label-position="top">
        <el-form-item label="知识库名称"><el-input v-model="form.name" maxlength="120" show-word-limit/></el-form-item>
        <el-form-item label="说明"><el-input v-model="form.description" type="textarea" :rows="4" maxlength="500" show-word-limit/></el-form-item>
      </el-form>
      <template #footer><el-button @click="dialogVisible=false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
    </el-dialog>
  </section>
</template>
