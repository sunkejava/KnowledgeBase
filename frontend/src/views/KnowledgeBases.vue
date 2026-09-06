<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { http } from '../api/http'

interface KnowledgeBaseItem { id: string; name: string; description: string; createdAt: string; updatedAt: string }
const items = ref<KnowledgeBaseItem[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const keyword = ref('')
const form = reactive({ name: '', description: '' })

async function load() {
  loading.value = true
  try { items.value = (await http.get('/knowledge-bases')).data }
  finally { loading.value = false }
}
function openCreate() { editingId.value = null; form.name = ''; form.description = ''; dialogVisible.value = true }
function openEdit(item: KnowledgeBaseItem) { editingId.value = item.id; form.name = item.name; form.description = item.description; dialogVisible.value = true }
async function save() {
  if (!form.name.trim()) return ElMessage.warning('请输入知识库名称')
  if (editingId.value) await http.put(`/knowledge-bases/${editingId.value}`, form)
  else await http.post('/knowledge-bases', form)
  dialogVisible.value = false
  ElMessage.success(editingId.value ? '知识库已更新' : '知识库已创建')
  await load()
}
async function remove(item: KnowledgeBaseItem) {
  await ElMessageBox.confirm(`确认删除知识库“${item.name}”？`, '删除确认', { type: 'warning' })
  await http.delete(`/knowledge-bases/${item.id}`)
  ElMessage.success('已删除')
  await load()
}
function visible(item: KnowledgeBaseItem) { const q = keyword.value.trim().toLowerCase(); return !q || item.name.toLowerCase().includes(q) || item.description.toLowerCase().includes(q) }
onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>我的知识库</h1><p>按业务边界管理团队知识、成员与权限。</p></div><button class="primary" @click="openCreate">创建知识库</button></div>
    <div class="toolbar"><input v-model="keyword" placeholder="搜索知识库"/><button @click="load">刷新</button></div>
    <div v-loading="loading" class="kb-grid">
      <article class="kb-card" v-for="kb in items.filter(visible)" :key="kb.id">
        <div class="kb-icon">KB</div><div class="kb-body"><h3>{{ kb.name }}</h3><p>{{ kb.description || '暂无描述' }}</p><footer><span>更新于 {{ new Date(kb.updatedAt).toLocaleString() }}</span></footer><div class="card-actions"><button @click="openEdit(kb)">编辑</button><button class="danger-text" @click="remove(kb)">删除</button></div></div>
      </article>
    </div>
    <div v-if="!loading && items.length === 0" class="empty-state">尚未创建知识库，从第一个知识空间开始沉淀团队资产。</div>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑知识库' : '创建知识库'" width="520px">
      <el-form label-position="top">
        <el-form-item label="知识库名称"><el-input v-model="form.name" maxlength="120" show-word-limit /></el-form-item>
        <el-form-item label="说明"><el-input v-model="form.description" type="textarea" :rows="4" maxlength="500" show-word-limit /></el-form-item>
      </el-form>
      <template #footer><el-button @click="dialogVisible=false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
    </el-dialog>
  </section>
</template>
