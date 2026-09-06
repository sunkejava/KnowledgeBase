<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import PageHeader from '../components/common/PageHeader.vue'
import type { TableColumn } from '../types/table'
import { knowledgeApi } from '../api/modules/knowledge'
import { documentApi } from '../api/modules/documents'
import { collaborationApi } from '../api/modules/collaboration'

const knowledgeBases = ref<any[]>([])
const documents = ref<any[]>([])
const selectedKnowledgeBaseId = ref('')
const selectedDocumentId = ref('')
const rows = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const loading = ref(false)
const keyword = ref('')
const commentText = ref('')
const mentionOptions = ref<any[]>([])
const mentionUserIds = ref<string[]>([])
const submitting = ref(false)

const columns: TableColumn<any>[] = [
  { key: 'displayName', label: '评论人', width: 130, formatter: row => row.displayName || row.userName },
  { key: 'content', label: '评论内容', minWidth: 420 },
  { key: 'mentionUserIds', label: '@人数', width: 90, formatter: row => String(row.mentionUserIds?.length || 0) },
  { key: 'createdAt', label: '评论时间', width: 190, formatter: row => new Date(row.createdAt).toLocaleString() }
]

const canComment = computed(() => Boolean(selectedDocumentId.value && commentText.value.trim()))

async function loadKnowledgeBases() {
  const { data } = await knowledgeApi.knowledgeBases(1, 100, '')
  knowledgeBases.value = data.items
  if (!selectedKnowledgeBaseId.value && data.items.length) selectedKnowledgeBaseId.value = data.items[0].id
}

async function loadDocuments() {
  documents.value = []
  selectedDocumentId.value = ''
  rows.value = []
  total.value = 0
  if (!selectedKnowledgeBaseId.value) return
  const { data } = await documentApi.list(selectedKnowledgeBaseId.value)
  documents.value = data
  if (data.length) selectedDocumentId.value = data[0].id
}

async function loadComments() {
  if (!selectedDocumentId.value) {
    rows.value = []
    total.value = 0
    return
  }
  loading.value = true
  try {
    const { data } = await collaborationApi.comments(selectedDocumentId.value, page.value, pageSize.value, keyword.value)
    rows.value = data.items
    total.value = data.total
  } finally {
    loading.value = false
  }
}

async function loadMentionUsers(searchKeyword = '') {
  if (!selectedDocumentId.value) return
  const { data } = await collaborationApi.mentionUsers(selectedDocumentId.value, searchKeyword, 30)
  mentionOptions.value = data
}

async function submitComment() {
  if (!canComment.value) return
  submitting.value = true
  try {
    await collaborationApi.createComment(selectedDocumentId.value, {
      content: commentText.value.trim(),
      parentId: null,
      mentionUserIds: mentionUserIds.value
    })
    commentText.value = ''
    mentionUserIds.value = []
    page.value = 1
    await loadComments()
    ElMessage.success('评论已发布')
  } finally {
    submitting.value = false
  }
}

async function removeComment(row: any) {
  await ElMessageBox.confirm('确认删除这条评论？', '删除评论', { type: 'warning' })
  try {
    await collaborationApi.deleteComment(row.id)
    await loadComments()
    ElMessage.success('评论已删除')
  } catch {
    ElMessage.warning('只能删除自己的评论，超级管理员除外')
  }
}

async function search() {
  page.value = 1
  await loadComments()
}

async function pageChange(nextPage: number, nextSize: number) {
  page.value = nextPage
  pageSize.value = nextSize
  await loadComments()
}

watch(selectedKnowledgeBaseId, async () => {
  page.value = 1
  await loadDocuments()
})
watch(selectedDocumentId, async () => {
  page.value = 1
  mentionUserIds.value = []
  await Promise.all([loadComments(), loadMentionUsers()])
})

onMounted(async () => {
  await loadKnowledgeBases()
  await loadDocuments()
})
</script>

<template>
  <section class="page">
    <PageHeader title="协作评论" description="围绕具体文档进行讨论，可 @知识库成员；被 @成员会收到站内通知。">
      <template #actions>
        <el-select v-model="selectedKnowledgeBaseId" filterable placeholder="选择知识库" style="width:240px">
          <el-option v-for="item in knowledgeBases" :key="item.id" :label="item.name" :value="item.id"/>
        </el-select>
        <el-select v-model="selectedDocumentId" filterable placeholder="选择文档" style="width:260px">
          <el-option v-for="item in documents" :key="item.id" :label="item.title" :value="item.id"/>
        </el-select>
      </template>
    </PageHeader>

    <el-card shadow="never" style="margin-bottom:16px">
      <el-input v-model="commentText" type="textarea" :rows="4" maxlength="4000" show-word-limit placeholder="输入评论内容…"/>
      <div style="display:flex;gap:12px;align-items:center;margin-top:12px">
        <el-select
          v-model="mentionUserIds"
          multiple
          filterable
          remote
          reserve-keyword
          :remote-method="loadMentionUsers"
          placeholder="@知识库成员"
          style="min-width:360px"
        >
          <el-option v-for="item in mentionOptions" :key="item.id" :label="`${item.displayName} (${item.userName})`" :value="item.id"/>
        </el-select>
        <el-button type="primary" :loading="submitting" :disabled="!canComment" @click="submitComment">发布评论</el-button>
      </div>
    </el-card>

    <div style="display:flex;gap:10px;margin-bottom:12px">
      <el-input v-model="keyword" clearable placeholder="搜索评论内容" style="width:260px" @keyup.enter="search"/>
      <el-button @click="search">查询</el-button>
    </div>

    <BaseDataTable
      :rows="rows"
      :columns="columns"
      :loading="loading"
      storage-key="document-comments"
      export-file-name="KnowledgeBase-文档评论"
      server-paging
      :total-count="total"
      :action-width="90"
      @refresh="loadComments"
      @page-change="pageChange"
    >
      <template #actions="{ row }"><el-button link type="danger" @click="removeComment(row)">删除</el-button></template>
    </BaseDataTable>
  </section>
</template>
