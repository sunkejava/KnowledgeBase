<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../common/BaseDataTable.vue'
import type { TableColumn } from '../../types/table'
import { accessApi } from '../../api/modules/access'

const props = defineProps<{
  modelValue: boolean
  knowledgeBaseId: string
  documentId?: string
}>()

const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()
const visible = computed({
  get: () => props.modelValue,
  set: value => emit('update:modelValue', value)
})

const tab = ref('members')
const loading = ref(false)
const members = ref<any[]>([])
const permissions = ref<any[]>([])
const users = ref<any[]>([])
const userKeyword = ref('')
const memberDialog = ref(false)
const permissionDialog = ref(false)
const memberForm = ref({ userId: '', role: 'Viewer' })
const permissionForm = ref({ userId: '', canView: true, canEdit: false, canManage: false })

const memberColumns: TableColumn<any>[] = [
  { key: 'displayName', label: '姓名', minWidth: 140 },
  { key: 'userName', label: '用户名', minWidth: 160 },
  { key: 'role', label: '知识库角色', width: 140 },
  { key: 'createdAt', label: '加入时间', width: 190, formatter: row => new Date(row.createdAt).toLocaleString() }
]
const permissionColumns: TableColumn<any>[] = [
  { key: 'displayName', label: '姓名', minWidth: 140 },
  { key: 'userName', label: '用户名', minWidth: 160 },
  { key: 'canView', label: '查看', width: 90, formatter: row => row.canView ? '是' : '否' },
  { key: 'canEdit', label: '编辑', width: 90, formatter: row => row.canEdit ? '是' : '否' },
  { key: 'canManage', label: '管理', width: 90, formatter: row => row.canManage ? '是' : '否' }
]

async function load() {
  if (!visible.value) return
  loading.value = true
  try {
    const tasks: Promise<any>[] = [accessApi.members(props.knowledgeBaseId)]
    if (props.documentId) tasks.push(accessApi.documentPermissions(props.documentId))
    const [memberData, permissionData] = await Promise.all(tasks)
    members.value = memberData.data
    permissions.value = permissionData?.data || []
  } finally {
    loading.value = false
  }
}

async function searchUsers() {
  users.value = (await accessApi.users(props.knowledgeBaseId, userKeyword.value, 50)).data
}

function onUserSearch(value: string) {
  userKeyword.value = value
  void searchUsers()
}

async function openMemberDialog(row?: any) {
  await searchUsers()
  memberForm.value = row ? { userId: row.userId, role: row.role } : { userId: '', role: 'Viewer' }
  memberDialog.value = true
}

async function saveMember() {
  if (!memberForm.value.userId) return ElMessage.warning('请选择用户')
  await accessApi.setMember(props.knowledgeBaseId, memberForm.value)
  memberDialog.value = false
  ElMessage.success('成员权限已保存')
  await load()
}

async function removeMember(row: any) {
  await ElMessageBox.confirm(`确认移除“${row.displayName}”的知识库成员权限？`, '移除确认', { type: 'warning' })
  await accessApi.removeMember(props.knowledgeBaseId, row.userId)
  await load()
}

async function openPermissionDialog(row?: any) {
  if (!props.documentId) return ElMessage.warning('请先选择文档')
  await searchUsers()
  permissionForm.value = row
    ? { userId: row.userId, canView: row.canView, canEdit: row.canEdit, canManage: row.canManage }
    : { userId: '', canView: true, canEdit: false, canManage: false }
  permissionDialog.value = true
}

async function savePermission() {
  if (!props.documentId || !permissionForm.value.userId) return ElMessage.warning('请选择用户')
  if (permissionForm.value.canManage) {
    permissionForm.value.canEdit = true
    permissionForm.value.canView = true
  } else if (permissionForm.value.canEdit) {
    permissionForm.value.canView = true
  }
  await accessApi.setDocumentPermission(props.documentId, permissionForm.value)
  permissionDialog.value = false
  ElMessage.success('文档权限已保存')
  await load()
}

async function removePermission(row: any) {
  if (!props.documentId) return
  await ElMessageBox.confirm(`确认移除“${row.displayName}”的文档显式权限？`, '移除确认', { type: 'warning' })
  await accessApi.removeDocumentPermission(props.documentId, row.userId)
  await load()
}

watch(() => props.modelValue, value => { if (value) load() })
watch(() => props.documentId, () => { if (visible.value) load() })
onMounted(() => { if (visible.value) load() })
</script>

<template>
  <el-drawer v-model="visible" title="资源权限管理" size="760px">
    <el-alert type="info" :closable="false" show-icon>
      文档显式权限优先于知识库成员角色。Manager 可维护成员与文档高级权限。
    </el-alert>

    <el-tabs v-model="tab" class="permission-tabs">
      <el-tab-pane label="知识库成员" name="members">
        <BaseDataTable
          :rows="members"
          :columns="memberColumns"
          :loading="loading"
          storage-key="knowledge-members"
          export-file-name="KnowledgeBase-知识库成员"
          :action-width="150"
          @refresh="load"
        >
          <template #toolbar><el-button type="primary" @click="openMemberDialog()">添加成员</el-button></template>
          <template #actions="{ row }">
            <el-button link type="primary" @click="openMemberDialog(row)">编辑</el-button>
            <el-button link type="danger" @click="removeMember(row)">移除</el-button>
          </template>
        </BaseDataTable>
      </el-tab-pane>

      <el-tab-pane label="当前文档权限" name="document" :disabled="!documentId">
        <BaseDataTable
          :rows="permissions"
          :columns="permissionColumns"
          :loading="loading"
          storage-key="document-permissions"
          export-file-name="KnowledgeBase-文档权限"
          :action-width="150"
          @refresh="load"
        >
          <template #toolbar><el-button type="primary" @click="openPermissionDialog()">添加权限</el-button></template>
          <template #actions="{ row }">
            <el-button link type="primary" @click="openPermissionDialog(row)">编辑</el-button>
            <el-button link type="danger" @click="removePermission(row)">移除</el-button>
          </template>
        </BaseDataTable>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="memberDialog" title="知识库成员权限" width="520px" append-to-body>
      <el-form label-position="top">
        <el-form-item label="用户">
          <el-select v-model="memberForm.userId" filterable remote :remote-method="onUserSearch" style="width:100%">
            <el-option v-for="item in users" :key="item.id" :label="`${item.displayName} (${item.userName})`" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="角色">
          <el-radio-group v-model="memberForm.role">
            <el-radio-button label="Viewer">Viewer</el-radio-button>
            <el-radio-button label="Editor">Editor</el-radio-button>
            <el-radio-button label="Manager">Manager</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer><el-button @click="memberDialog=false">取消</el-button><el-button type="primary" @click="saveMember">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="permissionDialog" title="文档显式权限" width="520px" append-to-body>
      <el-form label-position="top">
        <el-form-item label="用户">
          <el-select v-model="permissionForm.userId" filterable remote :remote-method="onUserSearch" style="width:100%">
            <el-option v-for="item in users" :key="item.id" :label="`${item.displayName} (${item.userName})`" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="权限">
          <el-checkbox v-model="permissionForm.canView">查看</el-checkbox>
          <el-checkbox v-model="permissionForm.canEdit">编辑</el-checkbox>
          <el-checkbox v-model="permissionForm.canManage">管理</el-checkbox>
        </el-form-item>
      </el-form>
      <template #footer><el-button @click="permissionDialog=false">取消</el-button><el-button type="primary" @click="savePermission">保存</el-button></template>
    </el-dialog>
  </el-drawer>
</template>

<style scoped>
.permission-tabs{margin-top:16px}
</style>
