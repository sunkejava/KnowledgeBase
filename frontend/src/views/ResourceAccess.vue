<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Share2, ShieldCheck } from 'lucide-vue-next'
import PageHeader from '../components/common/PageHeader.vue'
import ResourcePermissionPanel from '../components/knowledge/ResourcePermissionPanel.vue'
import ShareManagementPanel from '../components/knowledge/ShareManagementPanel.vue'
import { documentApi } from '../api/modules/documents'

const route=useRoute();const router=useRouter();const knowledgeBaseId=String(route.params.id)
const knowledgeBase=ref<any>(null);const documents=ref<any[]>([]);const documentId=ref('');const panelOpen=ref(false);const shareOpen=ref(false)
async function load(){const [base,docs]=await Promise.all([documentApi.knowledgeBase(knowledgeBaseId),documentApi.list(knowledgeBaseId)]);knowledgeBase.value=base.data;documents.value=docs.data}
onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="`${knowledgeBase?.name || '知识库'} · 资源管理`" description="统一维护成员、文档显式权限和公开分享安全策略。"><template #actions><el-button @click="router.push('/knowledge-bases')"><ArrowLeft :size="15"/>返回知识库</el-button></template></PageHeader>
    <div class="permission-overview">
      <div><span class="eyebrow">KNOWLEDGE BASE ACCESS</span><h3>成员权限</h3><p>Viewer 可查看，Editor 可编辑，Manager 可维护资源权限。</p><el-button type="primary" @click="documentId='';panelOpen=true"><ShieldCheck :size="15"/>管理知识库成员</el-button></div>
      <div><span class="eyebrow">DOCUMENT OVERRIDE</span><h3>文档显式权限</h3><p>为指定用户覆盖单篇文档的查看、编辑和管理权限。</p><el-select v-model="documentId" filterable placeholder="选择文档" style="width:100%;margin-bottom:12px"><el-option v-for="item in documents" :key="item.id" :label="item.title" :value="item.id"/></el-select><el-button :disabled="!documentId" @click="panelOpen=true">管理当前文档权限</el-button></div>
      <div><span class="eyebrow">PUBLIC SHARING</span><h3>分享安全</h3><p>创建密码保护/限时分享，查看访问次数与访问日志。</p><el-select v-model="documentId" filterable placeholder="选择文档" style="width:100%;margin-bottom:12px"><el-option v-for="item in documents" :key="item.id" :label="item.title" :value="item.id"/></el-select><el-button :disabled="!documentId" @click="shareOpen=true"><Share2 :size="15"/>管理分享</el-button></div>
    </div>
    <el-alert type="info" :closable="false" show-icon>资源权限与分享权限均由后端再次校验，前端按钮不是安全边界。</el-alert>
    <ResourcePermissionPanel v-model="panelOpen" :knowledge-base-id="knowledgeBaseId" :document-id="documentId || undefined"/>
    <ShareManagementPanel v-model="shareOpen" :document-id="documentId"/>
  </section>
</template>
<style scoped>.permission-overview{display:grid;grid-template-columns:repeat(3,1fr);gap:16px;margin-bottom:18px}.permission-overview>div{background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:22px}.permission-overview h3{margin:8px 0}.permission-overview p{color:var(--muted);font-size:13px;line-height:1.7;min-height:44px}.eyebrow{font-size:10px;letter-spacing:.12em;color:var(--muted)}@media(max-width:1100px){.permission-overview{grid-template-columns:1fr}}</style>
