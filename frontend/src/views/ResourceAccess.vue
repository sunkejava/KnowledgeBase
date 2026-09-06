<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, ShieldCheck } from 'lucide-vue-next'
import PageHeader from '../components/common/PageHeader.vue'
import ResourcePermissionPanel from '../components/knowledge/ResourcePermissionPanel.vue'
import { documentApi } from '../api/modules/documents'

const route = useRoute()
const router = useRouter()
const knowledgeBaseId = String(route.params.id)
const knowledgeBase = ref<any>(null)
const documents = ref<any[]>([])
const documentId = ref('')
const panelOpen = ref(false)

async function load() {
  const [base, docs] = await Promise.all([
    documentApi.knowledgeBase(knowledgeBaseId),
    documentApi.list(knowledgeBaseId)
  ])
  knowledgeBase.value = base.data
  documents.value = docs.data
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader
      :title="`${knowledgeBase?.name || '知识库'} · 权限管理`"
      description="维护知识库成员角色和单文档显式权限。文档显式权限优先于知识库成员角色。"
    >
      <template #actions>
        <el-button @click="router.push('/knowledge-bases')"><ArrowLeft :size="15" />返回知识库</el-button>
      </template>
    </PageHeader>

    <div class="permission-overview">
      <div>
        <span class="eyebrow">KNOWLEDGE BASE ACCESS</span>
        <h3>成员权限</h3>
        <p>Viewer 可查看，Editor 可查看和编辑，Manager 可管理知识库及成员权限。</p>
        <el-button type="primary" @click="documentId=''; panelOpen=true"><ShieldCheck :size="15" />管理知识库成员</el-button>
      </div>
      <div>
        <span class="eyebrow">DOCUMENT OVERRIDE</span>
        <h3>文档显式权限</h3>
        <p>为指定用户覆盖当前文档的查看、编辑和管理权限。</p>
        <el-select v-model="documentId" filterable placeholder="选择文档" style="width:100%;margin-bottom:12px">
          <el-option v-for="item in documents" :key="item.id" :label="item.title" :value="item.id" />
        </el-select>
        <el-button :disabled="!documentId" @click="panelOpen=true">管理当前文档权限</el-button>
      </div>
    </div>

    <el-alert type="info" :closable="false" show-icon>
      后端会在知识库、文档、附件、版本、Markdown 导入导出和分享接口中再次执行权限校验，前端按钮隐藏不是安全边界。
    </el-alert>

    <ResourcePermissionPanel
      v-model="panelOpen"
      :knowledge-base-id="knowledgeBaseId"
      :document-id="documentId || undefined"
    />
  </section>
</template>

<style scoped>
.permission-overview{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:18px}.permission-overview>div{background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:22px}.permission-overview h3{margin:8px 0}.permission-overview p{color:var(--muted);font-size:13px;line-height:1.7;min-height:44px}.eyebrow{font-size:10px;letter-spacing:.12em;color:var(--muted)}@media(max-width:900px){.permission-overview{grid-template-columns:1fr}}
</style>
